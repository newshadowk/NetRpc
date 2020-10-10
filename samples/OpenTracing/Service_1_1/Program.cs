using System;
using System.Threading;
using System.Threading.Tasks;
using DataContract;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NetRpc.Http;
using NetRpc.Jaeger;

namespace Service
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var h = Host.CreateDefaultBuilder(null)
                .ConfigureWebHostDefaults(builder =>
                {
                    builder.ConfigureKestrel((context, options) =>
                        {
                            options.ListenAnyIP(5104);
                            options.ListenAnyIP(50004, listenOptions => listenOptions.Protocols = HttpProtocols.Http2);
                        })
                        .ConfigureServices(services =>
                        {
                            services.AddCors();
                            services.AddSignalR();
                            services.AddNSwagger();
                            services.AddNHttpService();

                            services.AddNGrpcService();
                            services.AddNServiceContract<IService_1_1, Service>();

                            services.Configure<ServiceSwaggerOptions>(i => i.HostPath = "http://localhost:5104/swagger");
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
                            app.UseRouting();
                            app.UseEndpoints(endpoints =>
                            {
                                endpoints.MapHub<CallbackHub>("/callback");
                            });
                            app.UseNSwagger();
                            app.UseNHttp();
                            app.UseNGrpc();
                        });
                }).Build();

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