using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IoUring.Transport
{
    internal class IoUringConnectionListenerFactory : IConnectionListenerFactory
    {
        private readonly IoUringTransport _ioUringTransport;

        public IoUringConnectionListenerFactory(IoUringTransport ioUringTransport)
        {
            _ioUringTransport = ioUringTransport;
        }

        public ValueTask<IConnectionListener> BindAsync(EndPoint endpoint, CancellationToken cancellationToken = default)
            => IoUringConnectionListener.Create(endpoint, _ioUringTransport);
    }
}