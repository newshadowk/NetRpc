using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace NetRpc.Http
{
    public static class NManager
    {
        public static IWebHost CreateHost(int port, string hubPath, bool isSwagger, HttpServiceOptions httpServiceOptions, MiddlewareOptions middlewareOptions,
            params Contract[] contracts)
        {
            return WebHost.CreateDefaultBuilder(null)
#if NETCOREAPP2_1
                .UseKestrel(options => { options.ListenAnyIP(port); })
#else
                .ConfigureKestrel(options => { options.ListenAnyIP(port); })
#endif
                .ConfigureServices(services =>
                {
                    services.AddCors();
                    services.AddSignalR();
                    if (isSwagger)
                        services.AddNSwagger();
                    services.AddNHttpService(i =>
                    {
                        if (httpServiceOptions != null)
                        {
                            i.ApiRootPath = httpServiceOptions.ApiRootPath;
                            i.IgnoreWhenNotMatched = httpServiceOptions.IgnoreWhenNotMatched;
                        }
                    });
                    services.AddNRpcMiddleware(i =>
                    {
                        if (middlewareOptions != null)
                            i.AddItems(middlewareOptions.GetItems());
                    });

                    foreach (var contract in contracts)
                        services.AddNRpcServiceContract(contract.ContractInfo.Type, contract.InstanceType);
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
#if NETCOREAPP3_1
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapHub<CallbackHub>("/callback");
                    });
#else
                    app.UseSignalR(routes => { routes.MapHub<CallbackHub>(hubPath); });
#endif

                    if (isSwagger)
                        app.UseNSwagger();
                    app.UseNHttp();
                })
                .Build();
        }
    }
}