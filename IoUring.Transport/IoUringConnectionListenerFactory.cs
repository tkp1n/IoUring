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
        private IoUringOptions _options;
        private ILoggerFactory _loggerFactory;

        public IoUringConnectionListenerFactory(IOptions<IoUringOptions> options, ILoggerFactory loggerFactory)
        {
            _options = (options ?? throw new ArgumentNullException(nameof(options))).Value;
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        }
        
        public ValueTask<IConnectionListener> BindAsync(EndPoint endpoint, CancellationToken cancellationToken = default)
        {
            var listener = new IoUringConnectionListener(endpoint, _options, _loggerFactory);
            listener.Bind();
            return new ValueTask<IConnectionListener>(listener);
        }
    }
}