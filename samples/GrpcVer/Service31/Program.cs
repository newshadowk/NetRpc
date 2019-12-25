using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DataContract;
using Grpc.Core;
using GrpcService1;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetRpc.Http;
using Proxy.Grpc;

namespace Service
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var host = WebHost.CreateDefaultBuilder(null)
                .ConfigureKestrel(i =>
                {
                    //i.Limits.MaxRequestBodySize = 10737418240; //10G
                    //i.ListenAnyIP(5000, o => o.Protocols = HttpProtocols.Http2);
                    //i.ListenAnyIP(5001, o =>o.Protocols = HttpProtocols.Http1);
                    //i.ListenAnyIP(5001, listenOptions =>
                    //{
                    //    listenOptions.UseHttps(
                    //        @"1.pfx", "aaaa1111");
                    //});
                })
                .ConfigureServices(services =>
                {
                    services.AddCors();
                    services.AddSignalR();
                    services.AddNetRpcSwagger();
                    services.AddNetRpcHttpService();
                    services.AddNetRpcContractSingleton<IService, Service>();
                    services.AddNetRpcRabbitMQService(i => i.CopyFrom(TestHelper.Helper.GetMQOptions()));
                    //services.AddNetRpcGrpcService();

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
                    app.UseNetRpcSwagger();
                    app.UseNetRpcHttp();
                    app.UseMiddleware<SwaggerUiIndexMiddleware>();
                    //app.UseNetRpcGrpc();
                }).Build();

            await host.RunAsync();
        }
    }

    internal class Service : IService
    {
        public async Task Call(string s)
        {
            Console.WriteLine($"Receive: {s}");
        }

        public async Task<Stream> Echo(Stream s)
        {
            //using (var fw = File.OpenWrite(@"d:\testfile\servertgt.db"))
            //{
            //    await s.CopyToAsync(fw);
            //}

            MemoryStream ms = new MemoryStream();
            await s.CopyToAsync(ms);
            ms.Seek(0, SeekOrigin.Begin);
            return ms;
        }
    }

    public sealed class MessageCallImpl2 : MessageCall.MessageCallBase
    {

        public MessageCallImpl2()
        {
        }

        public override async Task DuplexStreamingServerMethod(IAsyncStreamReader<StreamBuffer> requestStream, IServerStreamWriter<StreamBuffer> responseStream,
            ServerCallContext context)
        {
            //Task.Run(async() =>
            //{
                try
                {
                    while (await requestStream.MoveNext(CancellationToken.None))
                        Console.WriteLine("count:" + requestStream.Current.Body.Length);
                    Console.WriteLine("end ---------");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            //});
        }
    }

    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddGrpc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                // Communication with gRPC endpoints must be made through a gRPC client.
                // To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909
                endpoints.MapGrpcService<GreeterService>();
            });
        }
    }

    public class GreeterService : Greeter.GreeterBase
    {
        private readonly ILogger<GreeterService> _logger;
        public GreeterService(ILogger<GreeterService> logger)
        {
            _logger = logger;
        }

        public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
        {
            Console.WriteLine("Hello " + request.Name);
            return Task.FromResult(new HelloReply
            {
                Message = "Hello " + request.Name
            });
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