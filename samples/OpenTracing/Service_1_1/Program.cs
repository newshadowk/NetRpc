using System;
using System.Threading.Tasks;
using DataContract;
using Grpc.Core;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NetRpc.Grpc;
using NetRpc.Http;
using NetRpc.Jaeger;

namespace Service
{
    class Program
    {
        const string origins = "_myAllowSpecificOrigins";

        static async Task Main(string[] args)
        {
            var h = WebHost.CreateDefaultBuilder(null)
                .ConfigureKestrel(options => { options.ListenAnyIP(5004); })
                .ConfigureServices(services =>
                {
                    services.AddCors(op =>
                    {
                        op.AddPolicy(origins, set =>
                        {
                            set.SetIsOriginAllowed(origin => true)
                                .AllowAnyHeader()
                                .AllowAnyMethod()
                                .AllowCredentials();
                        });
                    });

                    services.AddSignalR();
                    services.AddNetRpcSwagger();
                    services.AddNetRpcHttp();

                    services.AddNetRpcGrpcService(i => { i.AddPort("0.0.0.0", 50004); });
                    services.AddNetRpcContractSingleton<IService_1_1, Service>();

                    services.Configure<ServiceSwaggerOptions>(i => i.HostPath = "http://localhost:5004/swagger");
                    services.AddNetRpcJaeger(i =>
                    {
                        i.Host = "jaeger.yx.com";
                        i.Port = 6831;
                        i.ServiceName = "Service_1_1";
                    });
                })
                .Configure(app =>
                {
                    app.UseCors(origins);
                    app.UseSignalR(routes => { routes.MapHub<CallbackHub>("/callback"); });
                    app.UseNetRpcSwagger();
                    app.UseNetRpcHttp();
                })
                .Build();

            await h.RunAsync();
        }
    }

    internal class Service : IService_1_1
    {
        public async Task<Result> Call_1_1(int i1)
        {
            Console.WriteLine($"Receive: {i1}");
            return new Result();
        }
    }
}