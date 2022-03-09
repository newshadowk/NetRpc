using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DataContract;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetRpc.Http;
using NetRpc.Http.ShortConn;

namespace Service;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var port = 5000;
        if (args.Length == 1)
            port = int.Parse(args[0]);

        var host = Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.ConfigureKestrel((_, options) => { options.ListenAnyIP(port); })
                    .ConfigureServices((_, services) =>
                    {
                        services.AddCors();
                        services.AddSignalR();
                        services.AddNSwagger();
                        services.AddNHttpService();

                        services.Configure<HttpServiceOptions>(o =>
                        {
                            o.ShortConnRedisConnStr =
                                "192.168.0.50:6379,password=111,defaultDatabase=0,poolsize=50,ssl=false,writeBuffer=10240";
                            o.ShortConnTempDir = @"d:\1";
                        });
                        services.AddNGrpcClient(o => o.Url = "http://localhost:50001");
                        services.AddNClientContract<IService1Async>();
                        services.AddNHttpShortConn();
                        services.AddNServiceContract<IServiceAsync, ServiceAsync>();
                        services.AddNServiceContract<IService_, IService>();
                    }).Configure(app =>
                    {
                        app.UseCors(set =>
                        {
                            set.SetIsOriginAllowed(origin => true)
                                .AllowAnyHeader()
                                .AllowAnyMethod()
                                .AllowCredentials();
                        });

                        app.UseRouting();
                        app.UseEndpoints(endpoints => { endpoints.MapHub<CallbackHub>("/callback"); });
                        app.UseNSwagger();
                        app.UseNHttp();
                    }).ConfigureLogging(builder => builder.AddConsole());
            }).Build();

        await host.RunAsync();
    }
}

public class ServiceAsync : IServiceAsync
{
    private readonly IService1Async _p;

    public ServiceAsync(IService1Async p)
    {
        _p = p;
    }

    public async Task<CallResult> CallAsync(CallParam p, Stream stream, Func<double, Task> cb, CancellationToken token)
    {
        var ms = new MemoryStream();
        await stream.CopyToAsync(ms, token);

        //for (var i = 0; i < 1000; i++)
        //{
        //    Console.WriteLine($"-> prog {i}");
        //    await Task.Delay(2000, token);
        //    await cb(i);
        //}

        await Task.Delay(2000);

        var s = await _p.Call1Async("123");
        Console.WriteLine(s);
        ms.Seek(0, SeekOrigin.Begin);
        return new CallResult { P1 = "ret", Steam = ms, StreamName = p.StreamName };
    }
}

public class IService : IService_
{
    private readonly CacheHandler _cacheHandler;

    public IService(CacheHandler cacheHandler)
    {
        _cacheHandler = cacheHandler;
    }

    public Task<string> CallAsync(CallParam p, Stream stream)
    {
        return Task.FromResult(_cacheHandler.Start<IServiceAsync>("CallAsync", stream, p));
    }

    public async Task<CallResult?> CallResultAsync(string id)
    {
        return (CallResult?)await _cacheHandler.GetResultAsync(id);
    }
}
