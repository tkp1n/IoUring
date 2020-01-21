using System.Buffers;
using System.Collections.Generic;
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
        private const ulong ReadMask = 0x100000000UL;
        private const ulong WriteMask = 0x200000000UL;

        private readonly Ring _ring;
        private readonly IPEndPoint _endPoint;
        private readonly ChannelWriter<ConnectionContext> _acceptQueue;
        private readonly Dictionary<int, IoUringConnectionContext> _connections = new Dictionary<int, IoUringConnectionContext>();
        private readonly MemoryPool<byte> _memoryPool = new SlabMemoryPool();
        private LinuxSocket _acceptSocket;

        public TransportThread(IPEndPoint endPoint, ChannelWriter<ConnectionContext> acceptQueue)
        {
            _ring = new Ring(RingSize);
            _endPoint = endPoint;
            _acceptQueue = acceptQueue;
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
            new Thread(obj => ((TransportThread)obj).Loop()).Start(this);
        }

        private void Loop()
        {
            while (true)
            {
                Accept();

                foreach (var (socket, context) in _connections)
                {
                    Read(socket, context);
                    Write(socket, context);
                }

                Flush();
                Complete();
            }
        }

        private void Accept()
        {
            // TODO: accept using io_uring, once support landed in mainstream kernels
            // TODO: In the meantime, accept using epoll?
            var socket = _acceptSocket.Accept(out var endPoint);
            if (socket == -1)
            {
                if (_connections.Count == 0)
                {
                    // We don't have a connection yet to take care of -> spin wait until we have one
                    var wait = new SpinWait();
                    while ((socket = _acceptSocket.Accept(out var en)) == -1)
                    {
                        wait.SpinOnce();
                    }
                }
                else
                {
                    // No additional connections -> don't care
                    return;
                }
            }

            IoUringConnectionContext ctx = new IoUringConnectionContext(_endPoint, endPoint, _memoryPool);

            // TODO: create connection context

            _connections[socket] = ctx;
            _acceptQueue.TryWrite(ctx);
        }

        private unsafe void Read(LinuxSocket socket, IoUringConnectionContext context)
        {
            if (!context.ShouldRead) return;

            var writer = context.Input;
            var readHandles = context.ReadHandles;
            var readVecs = context.ReadVecs;
            for (int i = 0; i < IoUringConnectionContext.ReadIOVecCount; i++)
            {
                var memory = writer.GetMemory();
                var handle = memory.Pin(); // will be unpinned when read completes

                readVecs[i].iov_base = handle.Pointer;
                readVecs[i].iov_len = memory.Length;

                readHandles[i] = handle;
            }

            context.ShouldRead = false; // pause reading while the currently prepared op is in progress
            _ring.PrepareReadV(socket, readVecs, IoUringConnectionContext.ReadIOVecCount, 0, 0, ((ulong)(int)socket) | ReadMask);
        }

        private unsafe void Write(LinuxSocket socket, IoUringConnectionContext context)
        {
            if (!context.ShouldWrite) return;

            var reader = context.Output;
            if (!reader.TryRead(out var result)) return;

            // TODO: handle completed / cancelled

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
            context.ShouldWrite = false;
            _ring.PrepareWriteV(socket, writeVecs ,ctr, 0 ,0, (ulong)(int)socket | WriteMask);
        }

        private void Flush() => _ring.Flush(_ring.Submit());

        private void Complete()
        {
            Completion c = default;
            while (_ring.TryRead(ref c))
            {
                var socket = (int)c.userData;
                var context = _connections[socket];
                if ((c.userData & ReadMask) == ReadMask)
                {
                    CompleteRead(context, c.result);
                }
                else if ((c.userData & WriteMask) == WriteMask)
                {
                    CompleteWrite(context, c.result);
                }
            }
        }

        private static void CompleteRead(IoUringConnectionContext context, int result)
        {
            var readHandles = context.ReadHandles;
            for (int i = 0; i < readHandles.Length; i++)
            {
                readHandles[i].Dispose();
            }

            if (result > 0)
            {
                context.Input.Advance(result);
                var flushResult = context.Input.FlushAsync();
                context.FlushResult = flushResult;
                if (flushResult.IsCompleted)
                {
                    context.FlushedToApp();
                }
                else
                {
                    flushResult.GetAwaiter().UnsafeOnCompleted(context.OnFlushedToApp);
                }
            }
            else if (result < 0)
            {
                if (-result != EAGAIN)
                {
                    throw new ErrnoException(-result);
                }

                context.ShouldRead = true;
            }
        }

        private static void CompleteWrite(IoUringConnectionContext context, int result)
        {
            var writeHandles = context.WriteHandles;
            for (int i = 0; i < writeHandles.Length; i++)
            {
                writeHandles[i].Dispose();
            }

            if (result > 0)
            {
                var lastWrite = context.LastWrite;
                var end = lastWrite.Length == result ? lastWrite.End : lastWrite.GetPosition(result);

                context.Output.AdvanceTo(end);
                context.ShouldWrite = true;
            }
            else if (result < 0)
            {
                throw new ErrnoException(-result);
            }
        }
    }
}