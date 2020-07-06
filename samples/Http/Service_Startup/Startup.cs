using System;
using System.Threading.Tasks;
using DataContract;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using NetRpc;
using NetRpc.Http;

namespace Service_Startup
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors();
            services.AddSignalR();
            services.AddNSwagger();
            services.AddNHttpService();
            services.AddHttpContextAccessor();
            services.AddNRpcServiceContract<IServiceAsync, ServiceAsync>(ServiceLifetime.Scoped);
            services.AddNRpcMiddleware(i =>
            {
                i.UseMiddleware<A1Middleware>();
                i.UseMiddleware<A2Middleware>();
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseCors(set =>
            {
                set.SetIsOriginAllowed(origin => true)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.),
            // specifying the Swagger JSON endpoint.
            //app.UseSwaggerUI(c =>
            //{
            //    c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
            //});

            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<CallbackHub>("/callback");
            });
            app.UseNSwagger();
            app.UseNHttp();
        }
    }


    public class A1Middleware
    {
        private readonly RequestDelegate _next;


        public A1Middleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(ActionExecutingContext context)
        {
            Console.WriteLine("A1 start");
            await _next(context);
            Console.WriteLine("A1 end");
        }
    }

    public class A2Middleware
    {
        private readonly RequestDelegate _next;


        public A2Middleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(ActionExecutingContext context)
        {
            Console.WriteLine("A2 start");
            await _next(context);
            Console.WriteLine("A2 end");
        }
    }
}
