using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DataContract;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetRpc.Http;

namespace Service
{
    class Program
    {
        static async Task Main(string[] args)
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
                        }).ConfigureLogging(builder => builder.AddConsole()).Configure(app => { app.UseNGrpc(); });
                }).Build();

            await host.RunAsync();
        }
    }

    public class ServiceAsync : IServiceAsync
    {
        public async Task<string> CallAsync(string s)
        {
            Console.WriteLine($"Receive: {s}");
            return "call ret";
        }
    }
}