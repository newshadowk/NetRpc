using System;
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
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            var h = Host.CreateDefaultBuilder(null)
                .ConfigureWebHostDefaults(builder =>
                {
                    builder.ConfigureKestrel((context, options) =>
                        {
                            options.ListenAnyIP(5103);
                            options.ListenAnyIP(50003, listenOptions => listenOptions.Protocols = HttpProtocols.Http2);
                        })
                        .ConfigureServices(services =>
                        {
                            services.AddCors();

                            services.AddSignalR();
                            services.AddNSwagger();
                            services.AddNHttpService();

                            services.AddNGrpcService();
                            services.AddNServiceContract<IService_2, Service>();

                            services.Configure<ServiceSwaggerOptions>(i => i.HostPath = "http://localhost:5103/swagger");
                            services.AddNJaeger(i =>
                            {
                                i.Host = "m.k8s.yx.com";
                                i.Port = 36831;
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
                            app.UseRouting();
                            app.UseEndpoints(endpoints => { endpoints.MapHub<CallbackHub>("/callback"); });
                            app.UseNSwagger();
                            app.UseNHttp();
                            app.UseNGrpc();
                        });
                }).Build();
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