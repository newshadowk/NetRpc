using System.Threading.Tasks;
using DataContract;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
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
                    var services1 = services;
                    services1.AddCors(op =>
                    {
                        op.AddPolicy(origins, set =>
                        {
                            set.SetIsOriginAllowed(origin => true)
                                .AllowAnyHeader()
                                .AllowAnyMethod()
                                .AllowCredentials();
                        });
                    });

                    services1.AddSignalR();
                    services1.AddNetRpcSwagger();
                    services1.AddNetRpcHttpService();
                    services1.AddNetRpcContractSingleton<IServiceAsync, ServiceAsync>();
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