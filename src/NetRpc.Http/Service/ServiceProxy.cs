using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace NetRpc.Http
{
    public sealed class ServiceProxy : IDisposable
    {
        private readonly IWebHost _host;

        public ServiceProxy(int port, string hubPath, params object[] instances)
        {
            const string origins = "_myAllowSpecificOrigins";
            _host = WebHost.CreateDefaultBuilder(null)
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
                })
                .Configure(app =>
                {
                    app.UseCors(origins);
                    app.UseSignalR(routes => { routes.MapHub<CallbackHub>(hubPath); });
                    app.UseNetRpcHttp(instances);
                })
                .Build();
        }

        public void Open()
        {
            _host.Run();
        }

        public void Dispose()
        {
            _host.StartAsync().Wait();
        }
    }
}