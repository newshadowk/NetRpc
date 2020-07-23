using System;
using System.Threading;
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
        static async Task Main(string[] args)
        {
            var h = WebHost.CreateDefaultBuilder(null)
                .ConfigureKestrel(options => { options.ListenAnyIP(5004); })
                .ConfigureServices(services =>
                {
                    services.AddCors();
                    services.AddSignalR();
                    services.AddNSwagger();
                    services.AddNHttpService();

                    services.AddNGrpcService(i => { i.AddPort("0.0.0.0", 50004); });
                    services.AddNServiceContract<IService_1_1, Service>();

                    services.Configure<ServiceSwaggerOptions>(i => i.HostPath = "http://localhost:5004/swagger");
                    services.AddNJaeger(i =>
                    {
                        i.Host = "m.k8s.yx.com";
                        i.Port = 36831;
                        i.ServiceName = "Service_1_1";
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
                    app.UseNSwagger();
                    app.UseNHttp();
                })
                .Build();

            await h.RunAsync();
        }
    }

    internal class Service : IService_1_1
    {
        public async Task<Result> Call_1_1(int i1, Func<int, Task> cb, CancellationToken token)
        {
            for (var i = 0; i < 5; i++)
            {
                await cb.Invoke(i);
                await Task.Delay(100);
            }

            Console.WriteLine($"Receive: {i1}");
            return new Result();
        }
    }
}