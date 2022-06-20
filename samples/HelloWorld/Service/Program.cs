using System;
using System.Threading.Tasks;
using DataContract;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NetRpc;

namespace Service;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var grpcHost = Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.ConfigureKestrel((_, options) =>
                    {
                        options.ListenAnyIP(50001, listenOptions => listenOptions.Protocols = HttpProtocols.Http2);
                    })
                    .ConfigureServices((_, services) =>
                    {
                        services.AddNGrpcService();
                        services.AddNServiceContract<IServiceAsync, ServiceAsync>();
                    }).Configure(app => { app.UseNGrpc(); });
            }).Build();
        await grpcHost.RunAsync();
    }
}

public class ServiceAsync : IServiceAsync
{
    public async Task<string> CallAsync(string s)
    {
        GlobalDebugContext.Context.Info("call...");
        Console.WriteLine($"Receive: {s}");
        return s;
    }
}