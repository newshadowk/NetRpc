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
using NetRpc;
using NetRpc.Contract;
using NetRpc.Http;

namespace Service;

internal class Program
{
    private static async Task Main(string[] args)
    {
        int port = 5000;
        if (args.Length == 1)
            port = int.Parse(args[0]);

        var host = Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.ConfigureKestrel((_, options) =>
                    {
                        options.ListenAnyIP(port);
                    })
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
                    });
            }).Build();

        await host.RunAsync();
    }
}

public class ServiceAsync : IServiceAsync
{
    public async Task<CallResult> CallAsync(CallParam p, Stream stream, Func<double, Task> cb, CancellationToken token)
    {
        var ms = new MemoryStream();
        await stream.CopyToAsync(ms, token);

        for (var i = 0; i < 1000; i++)
        {
            Console.WriteLine($"-> prog {i}");
            await Task.Delay(2000, token);
            await cb(i);
        }

        ms.Seek(0, SeekOrigin.Begin);

        return new CallResult { P1 = "ret", Steam = ms, StreamName = p.StreamName};
    }
}

public class IService : IService_
{
    private readonly ShortConnCacheHandler _cacheHandler;

    public IService(ShortConnCacheHandler cacheHandler)
    {
        _cacheHandler = cacheHandler;
    }

    public async Task<string> CallAsync(CallParam p, Stream stream)
    {
        var id = Guid.NewGuid().ToString();
        _cacheHandler.Start(id, typeof(IServiceAsync).GetMethod("CallAsync")!.ToActionInfo(), (ProxyStream)stream, new[] { p },
            GlobalActionExecutingContext.Context!.Header);
        return id;
    } 

    public async Task<ContextData> CallProgressAsync(string id)
    {
        return await _cacheHandler.GetProgressAsync(id);
    }

    public Task CallCancel(string id)
    {
        _cacheHandler.Cancel(id);
        return Task.CompletedTask;
    }

    public async Task<CallResult?> CallResultAsync(string id)
    {
        return (CallResult?)await _cacheHandler.GetResultAsync(id);
    }
}