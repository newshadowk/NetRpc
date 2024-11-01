using System;
using System.IO;
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
        var host = Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.ConfigureKestrel((_, options) =>
                    {
                        options.ListenAnyIP(5000);
                        // options.ListenAnyIP(50001, listenOptions => listenOptions.Protocols = HttpProtocols.Http2);
                    })
                    .ConfigureServices((_, services) =>
                    {
                        services.AddCors();
                        services.AddSignalR();
                        services.AddNSwagger();
                        services.AddNHttpService();

                        services.AddNGrpcService();
                        services.AddNMiddleware(o => o.UseMiddleware<ValueFilterMiddleware>());
                        services.AddNServiceContract<IServiceAsync, ServiceAsync>();
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
                        app.UseNGrpc();
                    });
            }).Build();

        await host.RunAsync();
    }
}

public class ServiceAsync : IServiceAsync
{
    public async Task<string> CallAsync(A1 a1)
    {
        GlobalDebugContext.Context.Info("call...");
        return "1";
    }

    // public async Task<string> Call3Async(string s)
    // {
    //     return "";
    // }
    //
    // public async Task<string> Call2Async(Stream s)
    // {
    //     Console.WriteLine($"receive:{s.Length}");
    //     return "1";
    // }
}