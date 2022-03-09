using System.Threading.Tasks;
using DataContract;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Service;

internal class Program
{
    private static async Task Main(string[] args)
    {
        //grpc service
        var grpcHost = Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.ConfigureKestrel((_, options) => { options.ListenAnyIP(50001, listenOptions => listenOptions.Protocols = HttpProtocols.Http2); })
                    .ConfigureServices((_, services) =>
                    {
                        services.AddNGrpcService();
                        services.AddNServiceContract<IService1Async, Service1Async>();
                    }).Configure(app => { app.UseNGrpc(); });
            }).Build();
        await grpcHost.RunAsync();
    }
}

public class Service1Async : IService1Async
{
    public Task<string> Call1Async(string s)
    {
        return Task.FromResult(s);
    }
}