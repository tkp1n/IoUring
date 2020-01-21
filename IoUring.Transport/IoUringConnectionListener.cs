using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;

namespace IoUring.Transport
{
    internal class IoUringConnectionListener : IConnectionListener
    {
        private readonly IoUringOptions _options;
        private readonly ILoggerFactory _loggerFactory;

        private ChannelReader<ConnectionContext> _acceptQueue;

        public IoUringConnectionListener(EndPoint endpoint, IoUringOptions options, ILoggerFactory loggerFactory)
        {
            EndPoint = endpoint;
            _options = options;
            _loggerFactory = loggerFactory;
        }

        public EndPoint EndPoint { get; }

        public void Bind()
        {
            if (!(EndPoint is IPEndPoint)) throw new NotSupportedException();
            if (EndPoint.AddressFamily != AddressFamily.InterNetwork && EndPoint.AddressFamily != AddressFamily.InterNetworkV6) throw new NotSupportedException();

            var threads = new TransportThread[1/* TODO: Math.Min(Environment.ProcessorCount, 16)*/];
            var acceptQueue = Channel.CreateUnbounded<ConnectionContext>();
            _acceptQueue = acceptQueue.Reader;
            for (int i = 0; i < threads.Length; i++)
            {
                var thread = new TransportThread(EndPoint as IPEndPoint, acceptQueue.Writer);
                thread.Bind();
                thread.Run();
                threads[i] = thread;
            }
        }

        public ValueTask<ConnectionContext> AcceptAsync(CancellationToken cancellationToken = default) =>
            _acceptQueue.ReadAsync(cancellationToken);

        public ValueTask UnbindAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public ValueTask DisposeAsync()
        {
            throw new NotImplementedException();
        }
    }
}