using System;
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
        private const ulong ReadMask =  0x100000000UL;
        private const ulong WriteMask = 0x200000000UL;
        private const ulong PollMask =  0x400000000UL;
        private const ulong AcceptPollMask =  0x800000000UL;

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
                    Poll(socket, context);
                    Read(socket, context);
                    Write(socket, context);
                }

                Flush();
                Complete();
            }
        }

        private void Accept()
        {
            _ring.PreparePollAdd(_acceptSocket, (ushort) POLLIN, AcceptPollMask);
        }

        private void Poll(LinuxSocket socket, IoUringConnectionContext context)
        {
            if (!context.ShouldPoll) return;
            _ring.PreparePollAdd(socket, (ushort) POLLIN, (ulong) (int) socket | PollMask);
            context.ShouldPoll = false;
        }

        private unsafe void Read(LinuxSocket socket, IoUringConnectionContext context)
        {
            var readableBytes = context.ReadableBytes;
            if (readableBytes == 0) return;

            var maxBufferSize = _memoryPool.MaxBufferSize;
            var iovs = Math.DivRem(readableBytes, maxBufferSize, out var remainder);
            if (iovs >= IoUringConnectionContext.ReadIOVecCount)
            {
                iovs = IoUringConnectionContext.ReadIOVecCount;
                remainder = 0;
            }

            var writer = context.Input;
            var readHandles = context.ReadHandles;
            var readVecs = context.ReadVecs;
            int i;
            for (i = 0; i < iovs; i++)
            {
                var memory = writer.GetMemory(maxBufferSize);
                var handle = memory.Pin();

                readVecs[i].iov_base = handle.Pointer;
                readVecs[i].iov_len = memory.Length;

                readHandles[i] = handle;
                
                writer.Advance(maxBufferSize);
            }

            if (remainder > 0)
            {
                var memory = writer.GetMemory(remainder);
                var handle = memory.Pin();

                readVecs[i].iov_base = handle.Pointer;
                readVecs[i].iov_len = memory.Length;

                readHandles[i] = handle;  
                writer.Advance(remainder);
            }

            var vecsTotal = iovs + (remainder > 0 ? 1 : 0);
            _ring.PrepareReadV(socket, readVecs, vecsTotal, 0, 0, ((ulong)(int)socket) | ReadMask);
            context.ReadVecsLength = vecsTotal;
        }

        private unsafe void Write(LinuxSocket socket, IoUringConnectionContext context)
        {
            if (!context.ShouldWrite) return;

            var reader = context.Output;
            if (!reader.TryRead(out var result)) return;

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
            context.ShouldWrite = false;
            _ring.PrepareWriteV(socket, writeVecs ,ctr, 0 ,0, (ulong)(int)socket | WriteMask);
        }

        private void Flush() => _ring.Flush(_ring.Submit(), 1);

        private void Complete()
        {
            Completion c = default;
            while (_ring.TryRead(ref c))
            {
                var socket = (int)c.userData;
                if ((c.userData & AcceptPollMask) == AcceptPollMask)
                {
                    CompleteAcceptPoll();
                }
                if (!_connections.TryGetValue(socket, out var context)) continue;
                if ((c.userData & PollMask) == PollMask)
                {
                    CompletePoll(socket, context, c.result);
                }
                else if ((c.userData & ReadMask) == ReadMask)
                {
                    CompleteRead(socket, context, c.result);
                }
                else if ((c.userData & WriteMask) == WriteMask)
                {
                    CompleteWrite(context, c.result);
                }
            }
        }

        private void CompleteAcceptPoll()
        {
            var socket = _acceptSocket.Accept(out var endPoint);
            if (socket == -1)
            {
                Accept();
                return;
            }

            IoUringConnectionContext context = new IoUringConnectionContext(_endPoint, endPoint, _memoryPool);

            _connections[socket] = context;
            _acceptQueue.TryWrite(context);
        }
        
        private void CompletePoll(LinuxSocket socket, IoUringConnectionContext context, int result)
        {
            if (result >= 0)
            {
                var readableBytes = socket.GetReadableBytes();
                if (readableBytes > 0)
                {
                    context.ReadableBytes = readableBytes;
                }
                else
                {
                    context.ShouldPoll = true;
                }
            }
            else
            {
                int err = errno;
                if (err == EAGAIN || err == EINTR)
                {
                    context.ShouldPoll = true;
                }

                throw new ErrnoException(err);
            } 
        }

        private unsafe void CompleteRead(int socket, IoUringConnectionContext context, int result)
        {
            static void Flush(IoUringConnectionContext ctx)
            {
                var flushResult = ctx.Input.FlushAsync();
                ctx.FlushResult = flushResult;
                if (flushResult.IsCompleted)
                {
                    ctx.FlushedToApp();
                }
                else
                {
                    flushResult.GetAwaiter().UnsafeOnCompleted(ctx.OnFlushedToApp);
                }
            }

            if (result > 0)
            {
                if (result == context.ReadableBytes)
                {
                    var readHandles = context.ReadHandles;
                    for (int i = 0; i < readHandles.Length; i++)
                    {
                        readHandles[i].Dispose();
                    }

                    // we read everything
                    Flush(context);
                }
                else if (result < context.ReadableBytes)
                {
                    context.ReadableBytes -= result;
                    _ring.PrepareReadV(socket, context.ReadVecs, context.ReadVecsLength, result, 0, (ulong)socket | ReadMask);
                }
            }
            else if (result < 0)
            {
                if (-result != EAGAIN || -result != EWOULDBLOCK)
                {
                    throw new ErrnoException(-result);
                }
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