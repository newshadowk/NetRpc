using System;
using System.Threading.Tasks;
using DataContract;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Service
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureKestrel((_, options) =>
                        {
                            options.ListenAnyIP(50001, listenOptions => listenOptions.Protocols = HttpProtocols.Http2);
                        })
                        .ConfigureServices((_, services) =>
                        {
                            services.AddNGrpcService();
                            services.AddNServiceContract<IServiceAsync, ServiceAsync>();
                            services.AddNMiddleware(o => o.UseCallbackThrottling(1000));
                        }).Configure(app => { app.UseNGrpc(); });
                }).Build();
            await host.RunAsync();
        }
    }

    internal class ServiceAsync : IServiceAsync
    {
        public async Task CallAsync(Func<int, Task> cb)
        {
            for (var i = 0; i <= 20; i++)
            {
                await Task.Delay(100);
                try
                {
                    await cb.Invoke(i);
                    Console.WriteLine($"Send callback: {i}");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }
        }
    }
}