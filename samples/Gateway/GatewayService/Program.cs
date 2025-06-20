using System;
using System.Threading.Tasks;
using DataContract;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NetRpc.Http;
using TestHelper;

namespace Service;

internal class Program
{
    private static async Task Main(string[] args)
    {
        RunHttpAsync();
        RunRabbitMQAsync();
        RunGrpcAsync();
        Console.ReadLine();
    }

    private static async Task RunHttpAsync()
    {
        var host = Host.CreateDefaultBuilder(null)
            .ConfigureWebHostDefaults(builder =>
            {
                builder.ConfigureKestrel((context, options) => { options.ListenAnyIP(5000); }).ConfigureServices(services =>
                    {
                        services.AddCors();
                        services.AddSignalR();
                        services.AddNSwagger();
                        services.AddNHttpService();
                        services.AddNGrpcGateway<IServiceAsync>(o => { o.Url = "http://localhost:50001"; });
                        services.AddNGrpcGateway<IService2Async>();
                    })
                    .Configure(app =>
                    {
                        app.UseCors(i =>
                            {
                                i.AllowAnyHeader();
                                i.AllowAnyMethod();
                                i.AllowCredentials();
                            }
                        );

                        app.UseRouting();

                        app.UseNSwagger();
                        app.UseNHttp();
                    });
            }).Build();

        await host.RunAsync();
    }

    private static async Task RunRabbitMQAsync()
    {
        var host = new HostBuilder()
            .ConfigureServices((context, services) =>
            {
                //set single target by DI.
                services.AddNRabbitMQService(i => i.CopyFrom(Helper.GetMQOptions()));
                services.AddNGrpcGateway<IServiceAsync>(o => { o.Url = "http://localhost:50001"; });
                services.AddNGrpcGateway<IService2Async>();

                //set different target point.
                //var p1 = NManager.CreateClientProxy<IService>(new Channel("localhost", 50001, ChannelCredentials.Insecure)).Proxy;
                //var p2 = NManager.CreateClientProxy<IService2>(new Channel("localhost2", 50001, ChannelCredentials.Insecure)).Proxy;
                //services.AddNetRpcContractSingleton(typeof(IService), p1);
                //services.AddNetRpcContractSingleton(typeof(IService2), p2);
            })
            .Build();

        await host.RunAsync();
    }

    private static async Task RunGrpcAsync()
    {
        var host = Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.ConfigureKestrel((context, options) =>
                    {
                        options.ListenAnyIP(50000, listenOptions => listenOptions.Protocols = HttpProtocols.Http2);
                    })
                    .ConfigureServices((context, services) =>
                    {
                        services.AddNGrpcService();
                        services.AddNGrpcGateway<IServiceAsync>(options => options.Url = "http://localhost:50001");
                        services.AddNGrpcGateway<IService2Async>();
                    }).Configure(app => { app.UseNGrpc(); });
            })
            .Build();
        await host.RunAsync();
    }
}