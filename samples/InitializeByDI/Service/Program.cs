using System;
using System.Threading.Tasks;
using DataContract;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NetRpc;

namespace Service
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureKestrel((context, options) =>
                        {
                            options.ListenAnyIP(50001, listenOptions => listenOptions.Protocols = HttpProtocols.Http2);
                        })
                        .ConfigureServices((context, services) =>
                        {
                            services.AddNGrpcService();
                            services.AddNServiceContract<IServiceAsync, ServiceAsync>();
                        }).Configure(app => { app.UseNGrpc(); });
                }).Build();

            await host.RunAsync();
        }
    }

    internal class ServiceAsync : IServiceAsync
    {
        public static readonly Random _rd = new Random();

        public async Task<string> CallAsync(string s)
        {
            Console.WriteLine($"CallAsync {s}");
            await Task.Delay(_rd.Next(1, 500));
            //await Task.Delay(10000);
            Console.WriteLine($"CallAsync {s} end.");
            return s;
        }
    }
}