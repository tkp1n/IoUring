using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;
using Microsoft.AspNetCore.Connections;
using static Tmds.Linux.LibC;

namespace IoUring.Transport
{
    internal class TransportThread
    {
        private const int RingSize = 64;
        private const int ListenBacklog = 128;
        private const ulong ReadMask =        0x100000000UL << 0;
        private const ulong WriteMask =       0x100000000UL << 1;
        private const ulong ReadPollMask =    0x100000000UL << 2;
        private const ulong WritePollMask =   0x100000000UL << 3;
        private const ulong AcceptPollMask =  0x100000000UL << 4;
        private const ulong EventFdPollMask = 0x100000000UL << 5;

        private readonly Ring _ring;
        private readonly IPEndPoint _endPoint;
        private readonly ChannelWriter<ConnectionContext> _acceptQueue;
        private readonly ConcurrentQueue<IoUringConnectionContext> _readPollQueue = new ConcurrentQueue<IoUringConnectionContext>();
        private readonly ConcurrentQueue<IoUringConnectionContext> _writePollQueue = new ConcurrentQueue<IoUringConnectionContext>();
        private readonly Dictionary<int, IoUringConnectionContext> _connections = new Dictionary<int, IoUringConnectionContext>();
        private readonly TransportThreadContext _threadContext;
        private readonly MemoryPool<byte> _memoryPool = new SlabMemoryPool();
        private LinuxSocket _acceptSocket;
        private int _eventfd;

        public TransportThread(IPEndPoint endPoint, ChannelWriter<ConnectionContext> acceptQueue)
        {
            _ring = new Ring(RingSize);
            SetupEventFd();
            _endPoint = endPoint;
            _acceptQueue = acceptQueue;
            _threadContext = new TransportThreadContext(_readPollQueue, _writePollQueue, _eventfd);
        }

        public void Bind()
        {
            var domain = _endPoint.AddressFamily == AddressFamily.InterNetwork ? AF_INET : AF_INET6;
            LinuxSocket s = socket(domain, SOCK_STREAM | SOCK_NONBLOCK, IPPROTO_TCP);
            s.SetOption(SOL_SOCKET, SO_REUSEADDR, 1);
            s.SetOption(SOL_SOCKET, SO_REUSEPORT, 1);
            s.Bind(_endPoint);
            s.Listen(ListenBacklog);
            _acceptSocket = s;
        }

        public void Run()
        {
            if (_endPoint.AddressFamily == AddressFamily.InterNetworkV6) return; //TODO: remove (only for debugging)
            new Thread(obj => ((TransportThread)obj).Loop()).Start(this);
        }

        private void Loop()
        {
            PollEventFd();
            Accept();

            while (true)
            {
                while (_readPollQueue.TryDequeue(out var context))
                {
                    PollRead(context);
                }

                while (_writePollQueue.TryDequeue(out var context))
                {
                    PollWrite(context);
                }

                Flush();
                Complete();
            }
        }

        private void SetupEventFd()
        {
            int res = EventFd.eventfd(0, EventFd.EFD_SEMAPHORE);
            if (res == -1) throw new ErrnoException(errno);

            _ring.RegisterEventFd(res);
            _eventfd = res;
        }

        private void PollEventFd()
        {
            Debug.WriteLine("Adding poll on eventfd");
            _ring.PreparePollAdd(_eventfd, (ushort) POLLIN, EventFdPollMask);
        }

        private void Accept()
        {
            Debug.WriteLine($"Adding accept on {(int)_acceptSocket}");
            _ring.PreparePollAdd(_acceptSocket, (ushort) POLLIN, AcceptPollMask);
        }

        private void PollRead(IoUringConnectionContext context)
        {
            var socket = context.Socket;
            Debug.WriteLine($"Adding read poll on {(int)socket}");
            _ring.PreparePollAdd(socket, (ushort) POLLIN, Mask(socket, ReadPollMask));
        }

        private void PollWrite(IoUringConnectionContext context)
        {
            var socket = context.Socket;
            Debug.WriteLine($"Adding write poll on {(int)socket}");
            _ring.PreparePollAdd(socket, (ushort) POLLOUT, Mask(socket, WritePollMask));
        }

        private unsafe void Read(IoUringConnectionContext context)
        {
            var maxBufferSize = _memoryPool.MaxBufferSize;

            var writer = context.Input;
            var readHandles = context.ReadHandles;
            var readVecs = context.ReadVecs;

            var memory = writer.GetMemory(maxBufferSize);
            var handle = memory.Pin();

            readVecs[0].iov_base = handle.Pointer;
            readVecs[0].iov_len = memory.Length;

            readHandles[0] = handle;

            var socket = context.Socket;
            Debug.WriteLine($"Adding read on {(int)socket}");
            _ring.PrepareReadV(socket, readVecs, 1, 0, 0, Mask(socket, ReadMask));
        }

        private unsafe void Write(IoUringConnectionContext context)
        {
            var result = context.ReadResult.Result;

            var socket = context.Socket;
            if (result.IsCanceled || result.IsCompleted)
            {
                context.DisposeAsync();
                _connections.Remove(socket);
                socket.Close();
            }

            var writeHandles = context.WriteHandles;
            var writeVecs = context.WriteVecs;
            var buffer = result.Buffer;
            int ctr = 0;
            foreach (var memory in buffer)
            {
                var handle = memory.Pin();

                writeVecs[ctr].iov_base = handle.Pointer;
                writeVecs[ctr].iov_len = memory.Length;

                writeHandles[ctr] = handle;

                ctr++;
                if (ctr == IoUringConnectionContext.WriteIOVecCount) break;
            }

            context.LastWrite = buffer;
            Debug.WriteLine($"Adding write on {(int)socket}");
            _ring.PrepareWriteV(socket, writeVecs ,ctr, 0 ,0, Mask(socket, WriteMask));
        }

        private void Flush()
        {
            var submitted = _ring.Submit();
            if (submitted != 0)
            {
                Debug.WriteLine($"Waiting for one completion");
                _threadContext.IsBlocked = true;
                _ring.Flush(submitted, 1);
                _threadContext.IsBlocked = false;
            }
            else
            {
                // TODO: spin wait???
            }
        }

        private void Complete()
        {
            Completion c = default;
            while (_ring.TryRead(ref c))
            {
                var socket = (int)c.userData;
                if ((c.userData & EventFdPollMask) == EventFdPollMask)
                {
                    CompleteEventFdPoll();
                    continue;
                }
                if ((c.userData & AcceptPollMask) == AcceptPollMask)
                {
                    CompleteAcceptPoll();
                    continue;
                }
                if (!_connections.TryGetValue(socket, out var context)) continue;
                if ((c.userData & ReadPollMask) == ReadPollMask)
                {
                    CompleteReadPoll(context, c.result);
                }
                else if ((c.userData & WritePollMask) == WritePollMask)
                {
                    CompleteWritePoll(context, c.result);
                }
                else if ((c.userData & ReadMask) == ReadMask)
                {
                    CompleteRead(context, c.result);
                }
                else if ((c.userData & WriteMask) == WriteMask)
                {
                    CompleteWrite(context, c.result);
                }
            }
        }

        private void CompleteEventFdPoll()
        {
            Debug.WriteLine("EventFd completed");
            PollEventFd();
        }

        private void CompleteAcceptPoll()
        {
            var socket = _acceptSocket.Accept(out var endPoint);
            if (socket == -1)
            {
                Debug.WriteLine("Polled accept for nothing"); // TODO: remove
                goto AcceptAgain;
            }

            Debug.WriteLine($"Accepted {(int)socket}");
            // TODO: move server endpoint and memory pool to thread context
            IoUringConnectionContext context = new IoUringConnectionContext(socket, _endPoint, endPoint, _memoryPool, _threadContext);

            _connections[socket] = context;
            _acceptQueue.TryWrite(context);

            PollRead(context);
            ReadFromApp(context);

        AcceptAgain:
            Accept();
        }

        private void CompleteReadPoll(IoUringConnectionContext context, int result)
        {
            if (result < 0)
            {
                if (-result != EAGAIN || -result != EINTR)
                {
                    throw new ErrnoException(-result);
                }
                
                Debug.WriteLine("Polled read for nothing"); // TODO: remove
                PollRead(context);
                return;
            }
            
            Debug.WriteLine($"Completed read poll on {(int)context.Socket}");
            Read(context);
        }

        private void CompleteWritePoll(IoUringConnectionContext context, int result)
        {
            if (result < 0)
            {
                if (-result != EAGAIN || -result != EINTR)
                {
                    throw new ErrnoException(-result);
                }
                
                Debug.WriteLine("Polled write for nothing"); // TODO: remove
                PollWrite(context);
                return;
            }
            
            Debug.WriteLine($"Completed write poll on {(int)context.Socket}");
            Write(context);
        }

        private void CompleteRead(IoUringConnectionContext context, int result)
        {
            if (result > 0)
            {
                Debug.WriteLine($"Read {result} bytes from {(int)context.Socket}");
                context.Input.Advance(result);
                FlushRead(context);
            }
            else if (result < 0)
            {
                if (-result != EAGAIN || -result != EWOULDBLOCK || -result != EINTR)
                {
                    throw new ErrnoException(-result);
                }
                
                Debug.WriteLine("Read for nothing"); // TODO: remove
            }
        }

        private void FlushRead(IoUringConnectionContext context)
        {
            var flushResult = context.Input.FlushAsync();
            context.FlushResult = flushResult;
            if (flushResult.IsCompleted)
            {
                Debug.WriteLine($"Flushed read from {(int)context.Socket} synchronously");
                context.FlushedToAppSynchronously();
                PollRead(context);
            }
            else
            {
                flushResult.GetAwaiter().UnsafeOnCompleted(context.OnFlushedToApp);
            }
        }

        private void CompleteWrite(IoUringConnectionContext context, int result)
        {
            try
            {
                if (result > 0)
                {
                    var lastWrite = context.LastWrite;
                    var end = lastWrite.Length == result ? lastWrite.End : lastWrite.GetPosition(result);

                    Debug.WriteLine($"Wrote {result} bytes to {(int)context.Socket}");
                    context.Output.AdvanceTo(end);
                    ReadFromApp(context);
                }
                else if (result < 0)
                {
                    if (-result != EAGAIN && -result != EWOULDBLOCK && -result != EINTR)
                    {
                        throw new ErrnoException(-result);
                    }

                    Debug.WriteLine("Wrote for nothing"); // TODO: remove
                }
            }
            finally
            {
                var writeHandles = context.WriteHandles;
                for (int i = 0; i < writeHandles.Length; i++)
                {
                    writeHandles[i].Dispose();
                }
            }
        }

        private void ReadFromApp(IoUringConnectionContext context)
        {
            var readResult = context.Output.ReadAsync();
            context.ReadResult = readResult;
            if (readResult.IsCompleted)
            {
                Debug.WriteLine($"Read from app for {(int)context.Socket} synchronously");
                context.ReadFromAppSynchronously();
                PollWrite(context);
            }
            else
            {
                readResult.GetAwaiter().UnsafeOnCompleted(context.OnReadFromApp);
            }
        }

        private static ulong Mask(int socket, ulong mask)
        {
            var socketUl = (ulong)socket;
            return socketUl | mask;
        }
    }
}