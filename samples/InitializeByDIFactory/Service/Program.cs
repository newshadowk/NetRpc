using System;
using System.Threading.Tasks;
using DataContract;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TestHelper;

namespace Service;

internal class Program
{
    private static async Task Main(string[] args)
    {
        await RunAsync();
    }

    private static async Task RunAsync()
    {
        var host = Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.ConfigureKestrel((context, options) =>
                    {
                        options.ListenAnyIP(50001, listenOptions => listenOptions.Protocols = HttpProtocols.Http2);
                    })
                    .ConfigureServices((context, services) =>
                    {
                        services.AddNGrpcService();
                        services.AddNRabbitMQService(i => { i.CopyFrom(Helper.GetMQOptions()); });
                        services.AddNServiceContract<IService, Service>();
                    }).Configure(app => { app.UseNGrpc(); });
            })
            .Build();

        await host.RunAsync();
    }
}

internal class Service : IService
{
    public Task CallAsync(string s)
    {
        Console.WriteLine($"CallAsync {s}");
        return Task.CompletedTask;
    }
}