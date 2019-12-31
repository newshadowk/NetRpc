using System;
using System.Threading.Tasks;
using DataContract;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using NetRpc.Http;
using NetRpc.Jaeger;

namespace Service
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var h = WebHost.CreateDefaultBuilder(null)
                .ConfigureKestrel(options => { options.ListenAnyIP(5003); })
                .ConfigureServices(services =>
                {
                    services.AddCors();

                    services.AddSignalR();
                    services.AddNetRpcSwagger();
                    services.AddNetRpcHttpService();

                    services.AddNetRpcGrpcService(i => { i.AddPort("0.0.0.0", 50003); });
                    services.AddNetRpcServiceContract<IService_2, Service>();

                    services.Configure<ServiceSwaggerOptions>(i => i.HostPath = "http://localhost:5003/swagger");
                    services.AddNetRpcJaeger(i =>
                    {
                        i.Host = "jaeger.yx.com";
                        i.Port = 6831;
                        i.ServiceName = "Service_2";
                    });
                })
                .Configure(app =>
                {
                    app.UseCors(set =>
                    {
                        set.SetIsOriginAllowed(origin => true)
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .AllowCredentials();
                    });
                    app.UseSignalR(routes => { routes.MapHub<CallbackHub>("/callback"); });
                    app.UseNetRpcSwagger();
                    app.UseNetRpcHttp();
                })
                .Build();

            await h.RunAsync();
        }
    }

    internal class Service : IService_2
    {
        public async Task<Result> Call_2(bool b)
        {
            Console.WriteLine($"Receive: {b}");
            return new Result();
        }
    }
}