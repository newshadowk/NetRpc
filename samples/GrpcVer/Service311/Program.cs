using System;
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
                    //i.ConfigureEndpointDefaults(i => i.Protocols = HttpProtocols.Http2);
                    i.Limits.MaxRequestBodySize = 10737418240; //10G
                    i.ListenAnyIP(5000);
                })
                .Configure(app =>
                {
                    app.Use(async (context, next) =>
                    {
                        await context.Response.WriteAsync("1 start\r\n");
                        //调用管道中的下一个委托
                        await next.Invoke();
                        await context.Response.WriteAsync("1 end\r\n");
                    });

                    app.Use(async (context, next) =>
                    {
                        await context.Response.WriteAsync("2 start\r\n");
                        //调用管道中的下一个委托
                        await next.Invoke();
                        await context.Response.WriteAsync("2 end\r\n");
                    });
                    app.Run(async context =>
                    {
                        await context.Response.WriteAsync("run start\r\n");
                        await context.Response.WriteAsync("Hello from 2nd delegate.\r\n");
                        await context.Response.WriteAsync("run end\r\n");
                    });
                }).Build();

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
            Console.WriteLine("AMiddleware start");
            await _next(context);
            Console.WriteLine("AMiddleware end");
        }
    }

    public class BMiddleware
    {
        private readonly RequestDelegate _next;

        public BMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            Console.WriteLine("BMiddleware start");
            await _next(context);
            Console.WriteLine("BMiddleware end");
        }
    }
}