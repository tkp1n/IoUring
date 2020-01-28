using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;

namespace IoUring.Transport
{
    internal class IoUringConnectionListener : IConnectionListener
    {
        private readonly IoUringTransport _transport;

        private ChannelReader<ConnectionContext> _acceptQueue;

        private IoUringConnectionListener(EndPoint endpoint, IoUringTransport transport)
        {
            EndPoint = endpoint;
            _transport = transport;
        }

        public EndPoint EndPoint { get; }

        public static ValueTask<IConnectionListener> Create(EndPoint endpoint, IoUringTransport transport)
        {
            var listener = new IoUringConnectionListener(endpoint, transport);
            listener.Bind();
            return new ValueTask<IConnectionListener>(listener);
        }

        private void Bind()
        {
            if (!(EndPoint is IPEndPoint)) throw new NotSupportedException();
            if (EndPoint.AddressFamily != AddressFamily.InterNetwork && EndPoint.AddressFamily != AddressFamily.InterNetworkV6) throw new NotSupportedException();

            var acceptQueue = Channel.CreateUnbounded<ConnectionContext>();
            _acceptQueue = acceptQueue.Reader;

            var threads = _transport.TransportThreads;
            foreach (var thread in threads)
            {
                thread.Bind((IPEndPoint) EndPoint, acceptQueue.Writer);
            }
        }

        public async ValueTask<ConnectionContext> AcceptAsync(CancellationToken cancellationToken = default)
        {
            await foreach (var connection in _acceptQueue.ReadAllAsync(cancellationToken))
            {
                return connection;
            }

            return null;
        }

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