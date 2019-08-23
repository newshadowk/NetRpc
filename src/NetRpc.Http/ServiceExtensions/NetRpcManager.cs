using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace NetRpc.Http
{
    public static class NetRpcManager
    {
        public static IWebHost CreateHost(int port, string hubPath, bool isSwagger, 
            HttpServiceOptions httpServiceOptions, MiddlewareOptions middlewareOptions, params Type[] instanceTypes)
        {
            const string origins = "_myAllowSpecificOrigins";
            return WebHost.CreateDefaultBuilder(null)
                .ConfigureKestrel(options => { options.ListenAnyIP(port); })
                .ConfigureServices(services =>
                {
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
                    if (isSwagger)
                        services.AddNetRpcSwagger();
                    services.AddNetRpcHttp(i =>
                    {
                        if (httpServiceOptions != null)
                        {
                            i.ApiRootPath = httpServiceOptions.ApiRootPath;
                            i.IgnoreWhenNotMatched = httpServiceOptions.IgnoreWhenNotMatched;
                            i.IsClearStackTrace = httpServiceOptions.IsClearStackTrace;
                            i.SupportCallbackAndCancel = httpServiceOptions.SupportCallbackAndCancel;
                        }
                    });
                    services.AddNetRpcMiddleware(i =>
                    {
                        if (middlewareOptions != null)
                            i.Items = middlewareOptions.Items;
                    });
                    services.AddNetRpcServiceContract(instanceTypes);
                })
                .Configure(app =>
                {
                    app.UseCors(origins);
                    app.UseSignalR(routes => { routes.MapHub<CallbackHub>(hubPath); });
                    if (isSwagger)
                        app.UseNetRpcSwagger();
                    app.UseNetRpcHttp();
                })
                .Build();
        }
    }
}