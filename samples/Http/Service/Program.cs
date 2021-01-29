using System.Threading.Tasks;
using DataContract;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetRpc.Http;

namespace Service
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            var webHost = Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(builder =>
                {
                    builder.ConfigureKestrel((context, options) =>
                        {
                            options.Limits.MaxRequestBodySize = 10737418240; //10G
                            options.ListenAnyIP(5000);
                        })
                        .ConfigureServices(services =>
                        {
                            services.AddCors();
                            services.AddSignalR();
                            services.AddNSwagger(i =>
                            {
                                i.Items.Add(new KeyRole
                                {
                                    Key = "k1",
                                    Role = "R1"
                                });
                                i.Items.Add(new KeyRole
                                {
                                    Key = "k2",
                                    Role = "R1,R2"
                                });
                                i.Items.Add(new KeyRole
                                {
                                    Key = "k3",
                                    Role = "R3"
                                });
                                i.Items.Add(new KeyRole
                                {
                                    Key = "kall",
                                    Role = "RAll"
                                });
                            });
                            services.AddNMiniProfiler();
                            services.AddNHttpService();
                            services.AddNServiceContract<IService4Async, Service4Async>();
                            services.AddLogging(i => i.AddConsole());
                            //services.AddNServiceContract<IService2Async, Service2Async>();
                            //services.AddNServiceContract<IServiceAsync, ServiceAsync>();
                        })
                        .Configure(app =>
                        {
                            app.UseStaticFiles(new StaticFileOptions
                            {
                                FileProvider = new PhysicalFileProvider(@"d:\"),
                                RequestPath = "/doc"
                            });

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
                            app.UseNMiniProfiler();
                            app.UseNHttp();
                        });
                }).Build();


            await webHost.RunAsync();
        }
    }
}