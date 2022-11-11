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
using NetRpc.Http;

namespace Service;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.ConfigureKestrel((_, options) =>
                    {
                        options.ListenAnyIP(80);
                        options.ListenAnyIP(81, listenOptions => listenOptions.Protocols = HttpProtocols.Http2);
                    })
                    .ConfigureServices((_, services) =>
                    {
                        services.AddCors();
                        services.AddSignalR();
                        services.AddNSwagger();
                        services.AddNHttpService();
                        services.AddNGrpcService();
                        services.AddNServiceContract<IService1Async, Service1Async>();
                        services.AddHostedService<EInvoiceSenderService>();
                    }).Configure(app =>
                    {
                        app.UseCors(set =>
                        {
                            set.SetIsOriginAllowed(_ => true)
                                .AllowAnyHeader()
                                .AllowAnyMethod()
                                .AllowCredentials();
                        });

                        app.UseRouting();
                        app.UseEndpoints(endpoints => { endpoints.MapHub<CallbackHub>("/callback"); });

                        app.Use(static async (context, next) =>
                        {
                            context.Response.OnCompleted(async _ => { Console.WriteLine("OnCompleted"); }, null!);

                            await next();
                        });

                        app.UseNSwagger();
                        app.UseNHttp();
                        app.UseNGrpc();
                    }).ConfigureLogging(builder => builder.AddConsole());
            }).Build();

        await host.RunAsync();
    }
}

public static class G
{
    public static volatile bool Stopping;
}

public class EInvoiceSenderService : IHostedService
{
    public EInvoiceSenderService(IHostApplicationLifetime t)
    {
        t.ApplicationStopping.Register(() =>
        {
            G.Stopping = true;
            Console.WriteLine("ApplicationStopping start.");
            for (var i = 0; i < 5000; i++)
            {
                Console.WriteLine($"sleep {i}");
                Thread.Sleep(1000);
            }

            Console.WriteLine("ApplicationStopping end.");
        });
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("StartAsync");

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        //G.Stopping = true;
        //Console.WriteLine("StopAsync start.");
        //for (int i = 0; i < 5000; i++)
        //{
        //    Console.WriteLine($"sleep {i}");
        //    await Task.Delay(1000);
        //}
        //Console.WriteLine("StopAsync end.");
    }
}

public class Service1Async : IService1Async
{
    public async Task<string> Call1Async(string s)
    {
        //Console.WriteLine("in");
        //for (int i = 0; i < 100; i++)
        //{
        //    Console.WriteLine($"ing {i}");
        //    await Task.Delay(1000);
        //}
        //Console.WriteLine("out");
        throw new Exception("123");

        if (G.Stopping)
        {
            Console.WriteLine("---> Stopping, " + s);
            return s;
        }

        Console.WriteLine(s);
        return s;
    }

    public async Task<Stream> Call2Async(string s)
    {
        var fr = File.OpenRead(@"D:\TestFile\400MB.exe");
        return fr;
    }
}