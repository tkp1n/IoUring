using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Tmds.Linux;

namespace IoUring.Transport
{
    internal sealed unsafe class IoUringConnectionContext : TransportConnection
    {
        public const int ReadIOVecCount = 8;
        public const int WriteIOVecCount = 8;

        // Copied from LibuvTransportOptions.MaxReadBufferSize
        private const int PauseInputWriterThreshold = 1024 * 1024;
        // Copied from LibuvTransportOptions.MaxWriteBufferSize
        private const int PauseOutputWriterThreshold = 64 * 1024;

        private readonly Action _onOnFlushedToApp;

        private readonly iovec* _iovec;
        private GCHandle _iovecHandle;

        public IoUringConnectionContext(EndPoint server, EndPoint client, MemoryPool<byte> pool)
        {
            LocalEndPoint = server;
            RemoteEndPoint = client;

            MemoryPool = pool;

            var appScheduler = PipeScheduler.ThreadPool; // TODO: configure
            var inputOptions = new PipeOptions(MemoryPool, PipeScheduler.ThreadPool, PipeScheduler.Inline, PauseInputWriterThreshold, PauseInputWriterThreshold / 2, useSynchronizationContext: false);
            var outputOptions = new PipeOptions(MemoryPool, PipeScheduler.Inline, PipeScheduler.ThreadPool, PauseOutputWriterThreshold, PauseOutputWriterThreshold / 2, useSynchronizationContext: false);

            var pair = DuplexPipe.CreateConnectionPair(inputOptions, outputOptions);

            Transport = pair.Transport;
            Application = pair.Application;

            _onOnFlushedToApp = FlushedToApp;

            iovec[] vecs = new iovec[ReadIOVecCount + WriteIOVecCount];
            var handle = GCHandle.Alloc(vecs, GCHandleType.Pinned);
            _iovec = (iovec*) handle.AddrOfPinnedObject();
            _iovecHandle = handle;
        }

        public override MemoryPool<byte> MemoryPool { get; }

        public bool ShouldRead { get; set; } = true;
        public bool ShouldWrite { get; set; } = true;

        public iovec* ReadVecs => _iovec;
        public iovec* WriteVecs => _iovec + WriteIOVecCount;

        public MemoryHandle[] ReadHandles { get; } = new MemoryHandle[ReadIOVecCount];
        public MemoryHandle[] WriteHandles { get; } = new MemoryHandle[WriteIOVecCount];

        public ReadOnlySequence<byte> LastWrite { get; set; }

        public PipeWriter Input => Application.Output;

        public PipeReader Output => Application.Input;

        public ValueTask<FlushResult> FlushResult { get; set; }

        public Action OnFlushedToApp => _onOnFlushedToApp;

        public void FlushedToApp()
        {
            var flushResult = FlushResult;
            // TODO: handle result

            ShouldRead = true;
        }

        public override ValueTask DisposeAsync()
        {
            if (_iovecHandle.IsAllocated)
                _iovecHandle.Free();
            return base.DisposeAsync();
        }


        internal class DuplexPipe : IDuplexPipe
        {
            public DuplexPipe(PipeReader reader, PipeWriter writer)
            {
                Input = reader;
                Output = writer;
            }

            public PipeReader Input { get; }

            public PipeWriter Output { get; }

            public static DuplexPipePair CreateConnectionPair(PipeOptions inputOptions, PipeOptions outputOptions)
            {
                var input = new Pipe(inputOptions);
                var output = new Pipe(outputOptions);

                var transportToApplication = new DuplexPipe(output.Reader, input.Writer);
                var applicationToTransport = new DuplexPipe(input.Reader, output.Writer);

                return new DuplexPipePair(applicationToTransport, transportToApplication);
            }

            // This class exists to work around issues with value tuple on .NET Framework
            public readonly struct DuplexPipePair
            {
                public IDuplexPipe Transport { get; }
                public IDuplexPipe Application { get; }

                public DuplexPipePair(IDuplexPipe transport, IDuplexPipe application)
                {
                    Transport = transport;
                    Application = application;
                }
            }
        }
    }
}