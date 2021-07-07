using System;
using System.Threading.Tasks;
using DataContract;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NetRpc.Http;

namespace Service
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureKestrel((_, options) =>
                        {
                            options.ListenAnyIP(5000);
                            options.ListenAnyIP(50001, listenOptions => listenOptions.Protocols = HttpProtocols.Http2);
                        })
                        .ConfigureServices((_, services) =>
                        {
                            services.AddCors();
                            services.AddSignalR();
                            services.AddNSwagger();
                            services.AddNHttpService();

                            services.AddNGrpcService();
                            services.AddNServiceContract<IServiceAsync, ServiceAsync>();
                        }).Configure(app =>
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

            await host.RunAsync();
        }
    }

    public class ServiceAsync : IServiceAsync
    {
        public async Task<string> CallAsync(string s)
        {
            Console.WriteLine($"Receive: {s}");
            return s;
        }
    }
}