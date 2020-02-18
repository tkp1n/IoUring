using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using IoUring.Transport.Internals.Inbound;
using IoUring.Transport.Internals.Metrics;
using IoUring.Transport.Internals.Outbound;
using Microsoft.AspNetCore.Connections;
using Tmds.Linux;
using static Tmds.Linux.LibC;

namespace IoUring.Transport.Internals
{
    internal sealed class TransportThread
    {
        private const int RingSize = 4096;
        private const int ListenBacklog = 128;
        private const int MaxLoopsWithoutCompletion = 3;
        private const int MaxLoopsWithoutSubmission = 3;
        private const ulong ReadMask =        0x100000000UL << 0;
        private const ulong WriteMask =       0x100000000UL << 1;
        private const ulong ReadPollMask =    0x100000000UL << 2;
        private const ulong WritePollMask =   0x100000000UL << 3;
        private const ulong AcceptPollMask =  0x100000000UL << 4;
        private const ulong EventFdPollMask = 0x100000000UL << 5;

        private static int _threadId;

        private readonly Ring _ring;
        // TODO: One queue would probably suffice
        private readonly ConcurrentQueue<AcceptSocketContext> _acceptSocketQueue = new ConcurrentQueue<AcceptSocketContext>();
        private readonly ConcurrentQueue<IoUringConnectionContext> _clientSocketQueue = new ConcurrentQueue<IoUringConnectionContext>();
        private readonly ConcurrentQueue<IoUringConnectionContext> _readPollQueue = new ConcurrentQueue<IoUringConnectionContext>();
        private readonly ConcurrentQueue<IoUringConnectionContext> _writePollQueue = new ConcurrentQueue<IoUringConnectionContext>();
        // TODO: One Dictionary would probably suffice
        private readonly Dictionary<int, AcceptSocketContext> _acceptSockets = new Dictionary<int, AcceptSocketContext>();
        private readonly Dictionary<int, IoUringConnectionContext> _connections = new Dictionary<int, IoUringConnectionContext>();
        private readonly TransportThreadContext _threadContext;
        private readonly MemoryPool<byte> _memoryPool = new SlabMemoryPool();
        private int _eventfd;
        private GCHandle _eventfdBytes;
        private GCHandle _eventfdIoVec;

        // variables to prevent useless spinning in the event loop
        private int _loopsWithoutSubmission;
        private int _loopsWithoutCompletion;

        public TransportThread(IoUringOptions options)
        {
            _ring = new Ring(RingSize);
            SetupEventFd();
            _threadContext = new TransportThreadContext(options, _readPollQueue, _writePollQueue, _memoryPool, _eventfd);
        }

        public ValueTask<ConnectionContext> Connect(IPEndPoint endpoint)
        {
            var domain = endpoint.AddressFamily == AddressFamily.InterNetwork ? AF_INET : AF_INET6;
            LinuxSocket s = socket(domain, SOCK_STREAM | SOCK_NONBLOCK | SOCK_CLOEXEC, IPPROTO_TCP);
            if (_threadContext.Options.TcpNoDelay)
            {
                s.SetOption(SOL_TCP, TCP_NODELAY, 1);
            }
            var context = new OutboundConnectionContext(s, endpoint, _threadContext);

            bool connectedSynchronously;
            try
            {
                connectedSynchronously = s.TryConnectNonBlocking(endpoint);
            }
            catch (ErrnoException ex)
            {
                return new ValueTask<ConnectionContext>(Task.FromException<ConnectionContext>(ex));
            }

            if (connectedSynchronously)
            {
                context.LocalEndPoint = s.GetLocalAddress();
                return new ValueTask<ConnectionContext>(context);
            }

            var tcs = new TaskCompletionSource<ConnectionContext>();
            context.ConnectCompletion = tcs;

            // Socket will become writable, once connect completed
            _clientSocketQueue.Enqueue(context);
            _threadContext.Notify();

            return new ValueTask<ConnectionContext>(tcs.Task);
        }

        public void Bind(IPEndPoint endpoint, ChannelWriter<ConnectionContext> acceptQueue)
        {
            var domain = endpoint.AddressFamily == AddressFamily.InterNetwork ? AF_INET : AF_INET6;
            LinuxSocket s = socket(domain, SOCK_STREAM | SOCK_NONBLOCK | SOCK_CLOEXEC, IPPROTO_TCP);
            s.SetOption(SOL_SOCKET, SO_REUSEADDR, 1);
            s.SetOption(SOL_SOCKET, SO_REUSEPORT, 1);
            s.Bind(endpoint);
            s.Listen(ListenBacklog);

            var context = new AcceptSocketContext(s, endpoint, acceptQueue);

            _acceptSocketQueue.Enqueue(context);
            _threadContext.Notify();
        }

        public void Run() => new Thread(obj => ((TransportThread)obj).Loop())
        {
            IsBackground = true,
            Name = $"IoUring Transport Thread - {Interlocked.Increment(ref _threadId)}"
        }.Start(this);

        private void Loop()
        {
            ReadEventFd();

            while (true)
            {
                while (_acceptSocketQueue.TryDequeue(out var context))
                {
                    _acceptSockets[context.Socket] = context;
                    PollAccept(context);
                }
                while (_clientSocketQueue.TryDequeue(out var context))
                {
                    _connections[context.Socket] = context;
                    PollWrite(context);
                }
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

        private unsafe void SetupEventFd()
        {
            int res = eventfd(0, EFD_SEMAPHORE);
            if (res == -1) throw new ErrnoException(errno);
            _eventfd = res;

            // Pin buffer for eventfd reads via io_uring
            byte[] bytes = new byte[8];
            _eventfdBytes = GCHandle.Alloc(bytes, GCHandleType.Pinned);

            // Pin iovec used for eventfd reads via io_uring
            var eventfdIoVec = new iovec
            {
                iov_base = (void*) _eventfdBytes.AddrOfPinnedObject(),
                iov_len = bytes.Length
            };
            _eventfdIoVec = GCHandle.Alloc(eventfdIoVec, GCHandleType.Pinned);
        }

        private unsafe void ReadEventFd()
        {
            Debug.WriteLine("Adding read on eventfd");
            _ring.PrepareReadV(_eventfd, (iovec*) _eventfdIoVec.AddrOfPinnedObject(), 1, userData: EventFdPollMask);
        }

        private void PollAccept(AcceptSocketContext acceptSocket)
        {
            var socket = acceptSocket.Socket;
            Debug.WriteLine($"Adding accept on {(int)socket}");
            _ring.PreparePollAdd(socket, (ushort) POLLIN, Mask(socket, AcceptPollMask));
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
                return;
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
            IoUringTransportEventSource.Log.ReportSubmissionsPerEnter((int) submitted);

            uint minComplete = 0;
            if (_threadContext.BlockingMode)
            {
                minComplete = 1;
                IoUringTransportEventSource.Log.ReportBlockingEnter();
            }

            var flushed = _ring.Flush(submitted, minComplete);
            _threadContext.BlockingMode = false;

            if (flushed == 0)
            {
                _loopsWithoutSubmission++;
            }
            else
            {
                _loopsWithoutSubmission = 0;
            }
        }

        private void Complete()
        {
            var completions = 0;
            while (_ring.TryRead(out Completion c))
            {
                completions++;
                var socket = (int)c.userData;
                if ((c.userData & EventFdPollMask) == EventFdPollMask)
                {
                    CompleteEventFdPoll();
                    continue;
                }
                if ((c.userData & AcceptPollMask) == AcceptPollMask)
                {
                    CompleteAcceptPoll(_acceptSockets[socket]);
                    continue;
                }
                if (!_connections.TryGetValue(socket, out var context)) continue;
                if ((c.userData & ReadPollMask) == ReadPollMask)
                {
                    CompleteReadPoll(context, c.result);
                }
                else if ((c.userData & WritePollMask) == WritePollMask)
                {
                    if (context.ConnectCompletion is null)
                    {
                        CompleteWritePoll(context, c.result);
                    }
                    else
                    {
                        CompleteConnect(context, c.result);                        
                    }
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

            IoUringTransportEventSource.Log.ReportCompletionsPerEnter(completions);

            if (completions == 0)
            {
                _loopsWithoutCompletion++;
                if (_loopsWithoutSubmission >= MaxLoopsWithoutSubmission && 
                    _loopsWithoutCompletion >= MaxLoopsWithoutCompletion && 
                    !_threadContext.BlockingMode)
                {
                    // we might spin forever, if we don't act now
                    _threadContext.BlockingMode = true;
                }
            }
            else
            {
                _loopsWithoutCompletion = 0;
            }
        }

        private void CompleteEventFdPoll()
        {
            Debug.WriteLine("EventFd completed");
            ReadEventFd();
        }

        private void CompleteAcceptPoll(AcceptSocketContext context)
        {
            LinuxSocket socket;
    
            while ((socket = context.Socket.Accept(out var clientEndpoint)) != -1)
            {
                if (_threadContext.Options.TcpNoDelay)
                {
                    socket.SetOption(SOL_TCP, TCP_NODELAY, 1);
                }

                Debug.WriteLine($"Accepted {(int)socket}");
                var connectionContext = new InboundConnectionContext(socket, context.EndPoint, clientEndpoint, _threadContext);

                _connections[socket] = connectionContext;
                context.AcceptQueue.TryWrite(connectionContext);

                PollRead(connectionContext);
                ReadFromApp(connectionContext);
            }

            PollAccept(context);
        }

        private void CompleteConnect(IoUringConnectionContext context, int result)
        {
            var completion = context.ConnectCompletion;
            if (result < 0)
            {
                if (-result != EAGAIN || -result != EINTR)
                {
                    context.ConnectCompletion = null;
                    completion.TrySetException(new ErrnoException(-result));
                }

                PollWrite(context);
                return;
            }

            PollRead(context);
            ReadFromApp(context); // TODO once data is available, another useless write poll is submitted

            context.LocalEndPoint = context.Socket.GetLocalAddress();

            context.ConnectCompletion = null;
            completion.TrySetResult(context);
        }

        private void CompleteReadPoll(IoUringConnectionContext context, int result)
        {
            if (result < 0)
            {
                if (-result != EAGAIN || -result != EINTR)
                {
                    throw new ErrnoException(-result);
                }
                
                Debug.WriteLine("Polled read for nothing");
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
                
                Debug.WriteLine("Polled write for nothing");
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
                if (-result == ECONNRESET)
                {
                    context.DisposeAsync();
                } 
                else if (-result != EAGAIN || -result != EWOULDBLOCK || -result != EINTR)
                {
                    throw new ErrnoException(-result);
                }

                Debug.WriteLine("Read for nothing");
            }
            else
            {
                // TODO: handle connection closed
            }
        }

        private void FlushRead(IoUringConnectionContext context)
        {
            var flushResult = context.Input.FlushAsync();
            context.FlushResult = flushResult;
            if (flushResult.IsCompleted)
            {
                // likely
                Debug.WriteLine($"Flushed read from {(int)context.Socket} synchronously");
                IoUringTransportEventSource.Log.ReportSyncFlushAsync();
                context.FlushedToAppSynchronously();
                PollRead(context);
            }
            else
            {
                IoUringTransportEventSource.Log.ReportAsyncFlushAsync();
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
                    SequencePosition end;
                    if (lastWrite.Length == result)
                    {
                        Debug.WriteLine($"Wrote all {result} bytes to {(int)context.Socket}");
                        end = lastWrite.End;
                    }
                    else
                    {
                        Debug.WriteLine($"Wrote some {result} bytes to {(int)context.Socket}");
                        end = lastWrite.GetPosition(result);
                    }

                    context.Output.AdvanceTo(end);
                    ReadFromApp(context);
                }
                else if (result < 0)
                {
                    if (-result == ECONNRESET)
                    {
                        context.DisposeAsync();
                    } 
                    else if (-result != EAGAIN && -result != EWOULDBLOCK && -result != EINTR)
                    {
                        throw new ErrnoException(-result);
                    }

                    Debug.WriteLine("Wrote for nothing");
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
                IoUringTransportEventSource.Log.ReportSyncReadAsync();
                context.ReadFromAppSynchronously();
                PollWrite(context);
            }
            else
            {
                // likely
                IoUringTransportEventSource.Log.ReportAsyncReadAsync();
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