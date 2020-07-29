using System.Collections.Generic;
using System.Threading.Tasks;
using DataContract;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using NetRpc.Http;

namespace Service
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var webHost = WebHost.CreateDefaultBuilder(null)
                .ConfigureKestrel(i =>
                {
                    i.Limits.MaxRequestBodySize = 10737418240;   //10G
                    i.ListenAnyIP(5000);
                    i.ListenAnyIP(5001, listenOptions => { listenOptions.UseHttps(
                        @"1.pfx", "aaaa1111"); });
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
                            Role = "R2"
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
                    services.AddNHttpService();
                    services.AddNServiceContract<IService3Async, Service3Async>();
                })
                .Configure(app =>
                {
                    app.UseStaticFiles(new StaticFileOptions()
                    {
                        FileProvider = new PhysicalFileProvider(@"d:\"),
                        RequestPath = "/doc",
                    });

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
                })
                .Build();

            await webHost.RunAsync();
        }
    }
}