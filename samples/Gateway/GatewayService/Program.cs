using System;
using System.Threading.Tasks;
using DataContract;
using Grpc.Core;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NetRpc.Http;
using Helper = TestHelper.Helper;

namespace Service
{
    class Program
    {
        static async Task Main(string[] args)
        {
            RunHttpAsync();
            RunRabbitMQAsync();
            RunGrpcAsync();
            Console.ReadLine();
        }

        private static async Task RunHttpAsync()
        {
            var host = WebHost.CreateDefaultBuilder(null)
                .ConfigureKestrel(options => { options.ListenAnyIP(5000); })
                .ConfigureServices(services =>
                {
                    services.AddCors();
                    services.AddSignalR();
                    services.AddNetRpcSwagger();
                    services.AddNetRpcHttpService();
                    services.AddNetRpcGrpcGateway<IService>(o =>
                    {
                        o.Host = "localhost";
                        o.Port = 50001;
                    });
                    services.AddNetRpcGrpcGateway<IService2>();
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
                    app.UseSignalR(routes => { routes.MapHub<CallbackHub>("/callback"); });
                    app.UseNetRpcSwagger();
                    app.UseNetRpcHttp();
                })
                .Build();
            await host.RunAsync();
        }

        private static async Task RunRabbitMQAsync()
        {
            var host = new HostBuilder()
                .ConfigureServices((context, services) =>
                {
                    //set single target by DI.
                    services.AddNetRpcRabbitMQService(i => i.CopyFrom(Helper.GetMQOptions()));

                    services.AddNetRpcGrpcGateway<IService>(o =>
                    {
                        o.Host = "localhost";
                        o.Port = 50001;
                    });
                    services.AddNetRpcGrpcGateway<IService2>();

                    //set different target point.
                    //var p1 = NetRpcManager.CreateClientProxy<IService>(new Channel("localhost", 50001, ChannelCredentials.Insecure)).Proxy;
                    //var p2 = NetRpcManager.CreateClientProxy<IService2>(new Channel("localhost2", 50001, ChannelCredentials.Insecure)).Proxy;
                    //services.AddNetRpcContractSingleton(typeof(IService), p1);
                    //services.AddNetRpcContractSingleton(typeof(IService2), p2);
                })
                .Build();

            await host.RunAsync();
        }

        private static async Task RunGrpcAsync()
        {
            var host = new HostBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddNetRpcGrpcService(i => i.AddPort("0.0.0.0", 50000));
                    services.AddNetRpcGrpcGateway<IService>(o =>
                    {
                        o.Host = "localhost";
                        o.Port = 50001;
                    });
                    services.AddNetRpcGrpcGateway<IService2>();
                })
                .Build();

            await host.RunAsync();
        }
    }
}