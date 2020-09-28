using System;
using System.Threading.Tasks;
using DataContract;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NetRpc.Grpc;

namespace Service
{
    class Program
    {
        static async Task Main(string[] args)
        {
            //var host = Host.CreateDefaultBuilder(args)
            //    .ConfigureWebHostDefaults(webBuilder =>
            //    {
            //        webBuilder.ConfigureKestrel((context, options) =>
            //            {
            //                options.ListenAnyIP(50001, listenOptions => listenOptions.Protocols = HttpProtocols.Http2);
            //            })
            //            .ConfigureServices((context, services) =>
            //            {
            //                services.AddNGrpcService();
            //                services.AddNServiceContract<IService, Service>();
            //            }).Configure(app =>
            //            {
            //                app.UseNGrpc();
            //            });
            //    })
            //    .Build();
            //await host.RunAsync();

            var host = Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureKestrel((context, options) =>
                        {
                            options.ListenAnyIP(50001, listenOptions =>
                            {
                                listenOptions.Protocols = HttpProtocols.Http2;
                                listenOptions.UseHttps("server.pfx", "");
                            });
                        })
                        .ConfigureServices((context, services) => { services.AddGrpc(); }).Configure(app =>
                        {
                            app.UseRouting();
                            app.UseEndpoints(endpoints =>
                            {
                                endpoints.MapGrpcService<MessageCallImpl>();
                                endpoints.MapGet("/",
                                    async context =>
                                    {
                                        await context.Response.WriteAsync(
                                            "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
                                    });
                            });
                        });
                })
                .Build();
            await host.RunAsync();
        }
    }

    internal class Service : IService
    {
        public async Task Call(string s)
        {
            Console.WriteLine($"Receive: {s}");
        }
    }
}