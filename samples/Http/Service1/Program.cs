using System.Threading.Tasks;
using DataContract;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using NetRpc.Http;

namespace Service
{
    class Program
    {
        const string origins = "_myAllowSpecificOrigins";

        static async Task Main(string[] args)
        {
            var webHost = WebHost.CreateDefaultBuilder(null)
                .ConfigureServices(services =>
                {
                    //services.Configure<KestrelServerOptions>(i => i.AllowSynchronousIO = true);
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
                    services.AddNetRpcHttpService();
                    services.AddNetRpcContractSingleton<IServiceAsync, ServiceAsync>();
                })
                .Configure(app =>
                {
                    app.UseCors(origins);
                    app.UseSignalR(routes => { routes.MapHub<CallbackHub>("/callback"); });
                    app.UseNetRpcSwagger();
                    app.UseNetRpcHttp();
                })
                .Build();

            await webHost.RunAsync();
        }
    }
}