using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DataContract;
using Grpc.Core;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetRpc;
using NetRpc.Grpc;
using NetRpc.Http;
using NetRpc.Jaeger;
using NetRpc.OpenTracing;
using OpenTracing.Tag;
using OpenTracing.Util;
using Helper = TestHelper.Helper;

namespace Service
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var h = WebHost.CreateDefaultBuilder(null)
                .ConfigureKestrel(options => { options.ListenAnyIP(5001); })
                .ConfigureServices(services =>
                {
                    services.AddCors();
                    services.AddSignalR();
                    services.AddNetRpcSwagger();
                    services.AddNetRpcHttpService();

                    services.AddNetRpcRabbitMQService(i => i.CopyFrom(Helper.GetMQOptions()));
                    services.AddNetRpcServiceContract<IService, Service>(ServiceLifetime.Scoped);

                    services.Configure<GrpcClientOptions>("grpc1", i =>
                    {
                        i.Host = "localhost";
                        i.Port = 50002;
                    });
                    services.Configure<GrpcClientOptions>("grpc2", i =>
                    {
                        i.Host = "localhost";
                        i.Port = 50003;
                    });
                    services.AddNetRpcGrpcClient(null, null, ServiceLifetime.Scoped);

                    services.Configure<ServiceSwaggerOptions>(i => i.HostPath = "http://localhost:5001/swagger");
                    services.Configure<ClientSwaggerOptions>("grpc1", i => i.HostPath = "http://localhost:5002/swagger");
                    services.Configure<ClientSwaggerOptions>("grpc2", i => i.HostPath = "http://localhost:5003/swagger");
                    
                    services.AddNetRpcJaeger(i =>
                    {
                        i.Host = "jaeger.yx.com";
                        i.Port = 6831;
                        i.ServiceName = "Service";
                    }, i =>
                    {
                        i.LogActionInfoMaxLength = 1000;
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
                    app.UseNetRpcSwagger();
                    app.UseNetRpcHttp();
                })
                .ConfigureLogging(logging =>
                {
                    logging.AddConsole();
                })
                .Build();

            await h.RunAsync();
        }
    }

    public class T1
    {
        public string P1 { get; set; }
    }

    internal class Service : IService
    {
        private readonly IClientProxyFactory _factory;
        private readonly ILogger<Service> _logger;

        public Service(IClientProxyFactory factory, ILogger<Service> logger)
        {
            _factory = factory;
            _logger = logger;
        }

        public async Task<Result> Call(string s)
        {
            GlobalTracer.Instance.ActiveSpan.SetTag(new StringTag("file"), "123.txt");
            _logger.LogInformation($"Receive: {s}");
            var obj = new SendObj
            {
                InnerObj = new InnerObj
                {
                    IP1 = "ip1"
                },
                D1 = DateTime.Now,
                P1 = "p1",
                I1 = 100,
                B1 = true,
                BigList = new List<InnerObj>
                {
                    new InnerObj {IP1 = "1"},
                    new InnerObj {IP1 = "2"}
                }
            };

            using (var scope = TracerScope.BuildChild("test"))
            {
                scope.Span.SetTag("p1", "123");
                await Task.Delay(500);
            }

            try
            {
                throw new Exception("123");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "error msg");
            }

            await _factory.CreateProxy<IService_1>("grpc1").Proxy.Call_1(obj, 101, true,
                i => { _logger.LogInformation($"tid:{GlobalTracer.Instance?.ActiveSpan.Context.TraceId}, callback:{i}"); }, default);

            await _factory.CreateProxy<IService_2>("grpc2").Proxy.Call_2(false);

            return new Result();
        }

        public async Task<Stream> Echo(Stream stream)
        {
            //MemoryStream ms = new MemoryStream();
            //using (stream)
            //    await stream.CopyToAsync(ms);
            //ms.Seek(0, SeekOrigin.Begin);
            //return ms;
            return await _factory.CreateProxy<IService_1>("grpc1").Proxy.Echo_1(stream);
        }
    }
}