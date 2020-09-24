using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;


namespace NetRpc.Http
{
    public static class NManager
    {
        public static IWebHost CreateHost(int port, string hubPath, bool isSwagger, HttpServiceOptions? httpServiceOptions, MiddlewareOptions? middlewareOptions,
            params ContractParam[] contracts)
        {
            return WebHost.CreateDefaultBuilder(null)
                .ConfigureKestrel(options => { options.ListenAnyIP(port); })
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
                    services.AddNMiddleware(i =>
                    {
                        if (middlewareOptions != null)
                            i.AddItems(middlewareOptions.GetItems());
                    });

                    foreach (var contract in contracts)
                        services.AddNServiceContract(contract.ContractType, contract.InstanceType!);
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
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapHub<CallbackHub>("/callback");
                    });

                    if (isSwagger)
                        app.UseNSwagger();
                    app.UseNHttp();
                })
                .Build();
        }
    }
}