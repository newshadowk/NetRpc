using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DataContract;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetRpc;
using NetRpc.Contract;
using NetRpc.Grpc;
using NetRpc.Http;
using NetRpc.Http.Client;
using NetRpc.Jaeger;
using OpenTracing.Util;

namespace Service_1;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var h = Host.CreateDefaultBuilder(null)
            .ConfigureWebHostDefaults(builder =>
            {
                builder.ConfigureKestrel((context, options) =>
                    {
                        options.ListenAnyIP(5102);
                        options.ListenAnyIP(50002, listenOptions => listenOptions.Protocols = HttpProtocols.Http2);
                    })
                    .ConfigureServices(services =>
                    {
                        services.AddCors();

                        services.AddSignalR();
                        services.AddNSwagger();
                        services.AddNHttpService();
                        services.AddNGrpcService();
                        services.AddNServiceContract<IService_1, Service>();

                        services.Configure<GrpcClientOptions>("grpc", i => { i.Url = "http://localhost:50004"; });
                        services.Configure<HttpClientOptions>("http", i =>
                        {
                            i.ApiUrl = "http://localhost:5104";
                            i.SignalRHubUrl = "http://localhost:5104/callback";
                        });

                        services.AddNGrpcClient();
                        services.AddNHttpClient();

                        services.Configure<ServiceSwaggerOptions>(i => i.HostPath = "http://localhost:5102/swagger");
                        services.Configure<ClientSwaggerOptions>(i => i.HostPath = "http://localhost:5104/swagger");
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
                        app.UseRouting();
                        app.UseNSwagger();
                        app.UseNHttp();
                        app.UseNGrpc();
                    })
                    .ConfigureLogging(logging => { logging.AddConsole(); });
            }).Build();

        await h.RunAsync();
    }
}

internal class Service : IService_1
{
    private readonly IClientProxy<IService_1_1> _proxy;

    public Service(IClientProxyFactory factory)
    {
        _proxy = factory.CreateProxy<IService_1_1>("grpc");
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

        try
        {
            await _proxy.Proxy.Call_1_1(201, newCb, token);
        }
        catch (FaultException e)
        {
            var aa = e.Detail.StackTrace;
            Console.WriteLine(e);
            throw;
        }

        return new Result();
    }

    public async Task<Stream> Echo_1(Stream stream)
    {
        var ms = new MemoryStream();
        using (stream)
            await stream.CopyToAsync(ms);
        ms.Seek(0, SeekOrigin.Begin);
        return ms;
    }
}