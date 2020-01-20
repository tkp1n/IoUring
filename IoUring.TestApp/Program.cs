using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using IoUring.Transport;

namespace IoUring.TestApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseIoUring().UseStartup<Startup>(); });
    }
}