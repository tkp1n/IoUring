using System;
using IoUring.Transport.Internals;
using IoUring.Transport.Internals.Inbound;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.DependencyInjection;

namespace IoUring.Transport
{
    public static class ServiceCollectionIoUringExtensions
    {
        public static IServiceCollection AddIoUringTransport(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<IoUringTransport>();
            // TODO: Register IConnectionFactory for out-bound connections, once supported
            serviceCollection.AddSingleton<IConnectionListenerFactory, ConnectionListenerFactory>();

            return serviceCollection;
        }

        public static IServiceCollection AddIoUringTransport(this IServiceCollection serviceCollection, Action<IoUringOptions> options)
            => serviceCollection.Configure(options).AddIoUringTransport();
    }
}