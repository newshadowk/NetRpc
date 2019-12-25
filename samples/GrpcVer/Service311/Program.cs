using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace Service
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var host = WebHost.CreateDefaultBuilder(null)
                .ConfigureKestrel(i =>
                {
                    i.ConfigureEndpointDefaults(i => i.Protocols = HttpProtocols.Http2);
                    i.Limits.MaxRequestBodySize = 10737418240; //10G
                    i.ListenAnyIP(5000);
                })
                .Configure(app => { app.UseMiddleware<AMiddleware>(); }).Build();

            await host.RunAsync();
        }
    }

    public class AMiddleware
    {
        private readonly RequestDelegate _next;

        public AMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            await _next(context);
        }
    }
}