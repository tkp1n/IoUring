using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;

namespace IoUring.Transport.Internals.Inbound
{
    internal sealed class ConnectionListenerFactory : IConnectionListenerFactory
    {
        private readonly IoUringTransport _ioUringTransport;

        public ConnectionListenerFactory(IoUringTransport ioUringTransport)
        {
            _ioUringTransport = ioUringTransport;
        }

        public ValueTask<IConnectionListener> BindAsync(EndPoint endpoint, CancellationToken cancellationToken = default)
            => ConnectionListener.Create(endpoint, _ioUringTransport);
    }
}