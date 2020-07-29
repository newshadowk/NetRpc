using System.Threading.Tasks;
using DataContract;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using NetRpc;
using NetRpc.Http;

namespace Service
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var webHost = NManager.CreateHost(
                5000,
                "/callback",
                true,
                new HttpServiceOptions { ApiRootPath = "/api" },
                null,
                new ContractParam<IServiceAsync, ServiceAsync>());
            await webHost.RunAsync();

            //const string origins = "_myAllowSpecificOrigins";
            //var h = WebHost.CreateDefaultBuilder(null)
            //    .ConfigureServices(services =>
            //    {
            //        services.AddCors(op =>
            //        {
            //            op.AddPolicy(origins, set =>
            //            {
            //                set.SetIsOriginAllowed(origin => true)
            //                    .AllowAnyHeader()
            //                    .AllowAnyMethod()
            //                    .AllowCredentials();
            //            });
            //        });

            //        services.AddSignalR();
            //        services.AddNetRpcSwagger();
            //        services.AddNHttpService(i => i.ApiRootPath = "/api");
            //        services.AddNetRpcContractSingleton<IServiceAsync, ServiceAsync>();
            //    })
            //    .Configure(app =>
            //    {
            //        app.UseCors(origins);
            //        app.UseSignalR(routes => { routes.MapHub<CallbackHub>("/callback"); });
            //        app.UseNetRpcSwagger();
            //        app.UseNHttp();
            //    })
            //    .Build();

            //await h.RunAsync();
        }
    }
}