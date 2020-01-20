using System;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace IoUring.Transport
{
    public static class WebHostBuilderIoUringExtensions
    {
        public static IWebHostBuilder UseIoUring(this IWebHostBuilder builder) =>
            builder.ConfigureServices(services => services.AddSingleton<IConnectionListenerFactory, IoUringConnectionListenerFactory>());

        public static IWebHostBuilder UseIoUring(this IWebHostBuilder builder, Action<IoUringOptions> options) => 
            builder.UseIoUring().ConfigureServices(services => services.Configure(options));
    }
}