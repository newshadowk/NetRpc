using System.Net;
using System.Reflection;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using NetRpc.Http;

namespace Service
{
    class Program
    {
        static void Main(string[] args)
        {
            //there is two way to open a service:

            //1 use NetRpcManager create service.
            //var p = NetRpcManager.CreateServiceProxy(5000, "/callback", new Service());
            //p.Open();

            //2 use 'IApplicationBuilder.UseNetRpcHttp' to inject to your own exist service(like mvc web site).
            var webHost = GetWebHost();
            webHost.Run();
        }

        static IWebHost GetWebHost()
        {
            string origins = "_myAllowSpecificOrigins";
            string swaggerRootPath = "/s";

            return WebHost.CreateDefaultBuilder(null)
                .ConfigureKestrel(options => { options.ListenAnyIP(5000); })
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
                    services.AddDirectoryBrowser();
                })
                .Configure(app =>
                {
                    app.UseCors(origins);
                    app.UseSignalR(routes => { routes.MapHub<CallbackHub>("/callback"); });
                    app.UseNetRpcHttp(new Service());
                    app.UseMiddleware<SwaggerUiIndexMiddleware>(swaggerRootPath + "/index.html", "NetRpc.Http.SwaggerUi3.index.html");
                    app.UseFileServer(new FileServerOptions
                    {
                        RequestPath = new PathString(swaggerRootPath),
                        FileProvider = new EmbeddedFileProvider(typeof(SwaggerUiIndexMiddleware).GetTypeInfo().Assembly, "NetRpc.Http.SwaggerUi3"),
                        EnableDirectoryBrowsing = true
                    });
                 
                })
                .Build();
        }
    }
}
