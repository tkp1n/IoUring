using System;
using IoUring.Transport.Internals;
using IoUring.Transport.Internals.Inbound;
using IoUring.Transport.Internals.Outbound;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.DependencyInjection;

namespace IoUring.Transport
{
    public static class ServiceCollectionIoUringExtensions
    {
        public static IServiceCollection AddIoUringTransport(this IServiceCollection serviceCollection)
        {
            if (!OsCompatibility.IsCompatible) return serviceCollection;

            serviceCollection.AddSingleton<IoUringTransport>();
            serviceCollection.AddSingleton<IConnectionFactory, ConnectionFactory>();
            serviceCollection.AddSingleton<IConnectionListenerFactory, ConnectionListenerFactory>();

            return serviceCollection;
        }

        public static IServiceCollection AddIoUringTransport(this IServiceCollection serviceCollection, Action<IoUringOptions> options) => 
            !OsCompatibility.IsCompatible ? serviceCollection : serviceCollection.Configure(options).AddIoUringTransport();
    }
}