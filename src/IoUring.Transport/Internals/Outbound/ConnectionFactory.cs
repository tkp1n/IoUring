using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;

namespace IoUring.Transport.Internals.Outbound
{
    internal sealed class ConnectionFactory : IConnectionFactory
    {
        private readonly IoUringTransport _transport;
        private int _threadIndex = -1; // -1 so we start at 0 at first Increment

        public ConnectionFactory(IoUringTransport transport)
        {
            _transport = transport;
        }

        public ValueTask<ConnectionContext> ConnectAsync(EndPoint endpoint, CancellationToken cancellationToken = default)
        {
            if (!(endpoint is IPEndPoint ipEndPoint)) throw new NotSupportedException();
            if (endpoint.AddressFamily != AddressFamily.InterNetwork && endpoint.AddressFamily != AddressFamily.InterNetworkV6) throw new NotSupportedException();

            var threads = _transport.TransportThreads;
            var index = Interlocked.Increment(ref _threadIndex) % threads.Length;

            return threads[index].Connect(ipEndPoint);
        }
    }
}