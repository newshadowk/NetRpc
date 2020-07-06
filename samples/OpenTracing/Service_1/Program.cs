using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DataContract;
using Grpc.Core;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using NetRpc;
using NetRpc.Grpc;
using NetRpc.Http;
using NetRpc.Http.Client;
using NetRpc.Jaeger;
using OpenTracing.Util;

namespace Service_1
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var h = WebHost.CreateDefaultBuilder(null)
                .ConfigureKestrel(options => { options.ListenAnyIP(5002); })
                .ConfigureServices(services =>
                {
                    services.AddCors();

                    services.AddSignalR();
                    services.AddNSwagger();
                    services.AddNHttpService();

                    services.AddNGrpcService(i => { i.AddPort("0.0.0.0", 50002); });
                    services.AddNRpcServiceContract<IService_1, Service>();

                    services.Configure<GrpcClientOptions>("grpc",
                        i =>
                        {
                            i.Host = "localhost";
                            i.Port = 50004;
                        });
                    services.Configure<HttpClientOptions>("http",
                        i =>
                        {
                            i.ApiUrl = "http://localhost:5004";
                            i.SignalRHubUrl = "http://localhost:5004/callback";
                        });

                    services.AddNGrpcClient();
                    services.AddNHttpClient();

                    services.Configure<ServiceSwaggerOptions>(i => i.HostPath = "http://localhost:5002/swagger");
                    services.Configure<ClientSwaggerOptions>(i => i.HostPath = "http://localhost:5004/swagger");
                    services.AddNJaeger(i =>
                    {
                        i.Host = "m.k8s.yx.com";
                        i.Port = 36831;
                        i.ServiceName = "Service_1";
                    });
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
                    app.UseSignalR(routes => { routes.MapHub<CallbackHub>("/callback"); });
                    app.UseNSwagger();
                    app.UseNHttp();
                })
                .Build();

            await h.RunAsync();
        }
    }

    internal class Service : IService_1
    {
        private readonly IClientProxy<IService_1_1> _proxy;

        public Service(IClientProxyFactory factory)
        {
            _proxy = factory.CreateProxy<IService_1_1>("http");
        }

        public async Task<Result> Call_1(SendObj s, int i1, bool b1, Func<int, Task> cb, CancellationToken token)
        {
            //throw new Exception();
            //for (var i = 0; i < 5; i++)
            //{
            //    cb.Invoke(i);
            //    await Task.Delay(100);
            //}

            Console.WriteLine($"Receive: {s}");
            Func<int, Task> newCb = async i =>
            {
                Console.WriteLine("tid:" + GlobalTracer.Instance.ActiveSpan.Context.TraceId);
                await cb(i);
            };
            await _proxy.Proxy.Call_1_1(201, newCb, token);
            return new Result();
        }

        public async Task<Stream> Echo_1(Stream stream)
        {
            MemoryStream ms = new MemoryStream();
            using (stream)
                await stream.CopyToAsync(ms);
            ms.Seek(0, SeekOrigin.Begin);
            return ms;
        }
    }
}