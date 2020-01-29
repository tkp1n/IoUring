using System;
using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Tmds.Linux;

namespace IoUring.Transport.Internals
{
    internal sealed unsafe class IoUringConnectionContext : TransportConnection
    {
        public const int ReadIOVecCount = 1;
        public const int WriteIOVecCount = 8;

        // Copied from LibuvTransportOptions.MaxReadBufferSize
        private const int PauseInputWriterThreshold = 1024 * 1024;
        // Copied from LibuvTransportOptions.MaxWriteBufferSize
        private const int PauseOutputWriterThreshold = 64 * 1024;

        private readonly TransportThreadContext _threadContext;
        private readonly Action _onOnFlushedToApp;
        private readonly Action _onReadFromApp;

        private readonly iovec* _iovec;
        private GCHandle _iovecHandle;

        public IoUringConnectionContext(LinuxSocket socket, EndPoint server, EndPoint client, TransportThreadContext threadContext)
        {
            Socket = socket;

            LocalEndPoint = server;
            RemoteEndPoint = client;

            MemoryPool = threadContext.MemoryPool;
            _threadContext = threadContext;

            var appScheduler = threadContext.Options.ApplicationSchedulingMode;
            var inputOptions = new PipeOptions(MemoryPool, appScheduler, PipeScheduler.Inline, PauseInputWriterThreshold, PauseInputWriterThreshold / 2, useSynchronizationContext: false);
            var outputOptions = new PipeOptions(MemoryPool, PipeScheduler.Inline, appScheduler, PauseOutputWriterThreshold, PauseOutputWriterThreshold / 2, useSynchronizationContext: false);

            var pair = DuplexPipe.CreateConnectionPair(inputOptions, outputOptions);

            Transport = pair.Transport;
            Application = pair.Application;

            _onOnFlushedToApp = FlushedToAppAsynchronously;
            _onReadFromApp = ReadFromAppAsynchronously;

            iovec[] vecs = new iovec[ReadIOVecCount + WriteIOVecCount];
            var handle = GCHandle.Alloc(vecs, GCHandleType.Pinned);
            _iovec = (iovec*) handle.AddrOfPinnedObject();
            _iovecHandle = handle;
        }

        public LinuxSocket Socket { get; }
        public override MemoryPool<byte> MemoryPool { get; }

        public iovec* ReadVecs => _iovec;
        public iovec* WriteVecs => _iovec + ReadIOVecCount;

        public MemoryHandle[] ReadHandles { get; } = new MemoryHandle[ReadIOVecCount];
        public MemoryHandle[] WriteHandles { get; } = new MemoryHandle[WriteIOVecCount];

        public ReadOnlySequence<byte> LastWrite { get; set; }

        public PipeWriter Input => Application.Output;

        public PipeReader Output => Application.Input;

        public ValueTask<FlushResult> FlushResult { get; set; }
        public ValueTask<ReadResult> ReadResult { get; set; }
        public Action OnFlushedToApp => _onOnFlushedToApp;
        public Action OnReadFromApp => _onReadFromApp;

        private void FlushedToApp(bool async)
        {
            var flushResult = FlushResult;
            // TODO: handle result

            var readHandles = ReadHandles;
            readHandles[0].Dispose();

            if (async)
            {
                Debug.WriteLine($"Flushed to app for {(int)Socket} asynchronously");
                _threadContext.ReadPollQueue.Enqueue(this);
            }
        }
        
        public void FlushedToAppSynchronously() => FlushedToApp(false);
        private void FlushedToAppAsynchronously() => FlushedToApp(true);

        private void ReadFromApp(bool async)
        {
            var readResult = ReadResult;
            // TODO: handle result

            if (async)
            {
                Debug.WriteLine($"Read from app for {(int)Socket} asynchronously");
                var blocking = _threadContext.BlockingMode;
                _threadContext.WritePollQueue.Enqueue(this);

                if (blocking)
                {
                    Debug.WriteLine("Attempting to unblock thread");
                    _threadContext.Unblock();
                }
            }            
        }

        private void ReadFromAppAsynchronously() => ReadFromApp(true);
        public void ReadFromAppSynchronously() => ReadFromApp(false);
        
        public override ValueTask DisposeAsync()
        {
            // TODO: close pipes?
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