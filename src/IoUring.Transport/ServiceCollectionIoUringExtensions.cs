using System;
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
            serviceCollection.AddSingleton<IConnectionListenerFactory, IoUringConnectionListenerFactory>();

            return serviceCollection;
        }

        public static IServiceCollection AddIoUringTransport(this IServiceCollection serviceCollection, Action<IoUringOptions> options)
            => serviceCollection.Configure(options).AddIoUringTransport();
    }
}