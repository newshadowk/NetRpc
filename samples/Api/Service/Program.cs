using System;
using System.Threading.Tasks;
using DataContract;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NetRpc.Http;
using Helper = TestHelper.Helper;

namespace Service
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            //rabbitMQ
            var mpHost = new HostBuilder()
                .ConfigureServices((_, services) =>
                {
                    services.AddNRabbitMQService(i => i.CopyFrom(Helper.GetMQOptions()));
                    services.AddNServiceContract<IServiceAsync, ServiceAsync>();
                    services.AddNServiceContract<IService, Service>();
                })
                .Build();
            mpHost.RunAsync();

            //grpc
            var grpcHost = Host.CreateDefaultBuilder()
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
                            services.AddNServiceContract<IService, Service>();
                        }).Configure(app => { app.UseNGrpc(); });
                }).Build();
            grpcHost.RunAsync();

            //http
            var httpHost = Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(builder =>
                {
                    builder.ConfigureKestrel((_, options) =>
                    {
                        options.Limits.MaxRequestBodySize = 10737418240; //10G
                        options.ListenAnyIP(50002);
                    })
                        .ConfigureServices(services =>
                        {
                            services.AddCors();
                            services.AddSignalR();
                            services.AddNHttpService();
                            services.AddNServiceContract<IServiceAsync, ServiceAsync>();
                            services.AddNServiceContract<IService, Service>();
                        })
                        .Configure(app =>
                        {
                            app.UseCors(set =>
                            {
                                set.SetIsOriginAllowed(_ => true)
                                    .AllowAnyHeader()
                                    .AllowAnyMethod()
                                    .AllowCredentials();
                            });

                            app.UseRouting();
                            app.UseEndpoints(endpoints => { endpoints.MapHub<CallbackHub>("/callback"); });
                            app.UseNHttp();
                        });
                }).Build();
            httpHost.RunAsync();

            Console.Read();
        }
    }
}