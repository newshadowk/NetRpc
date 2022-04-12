using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DataContract;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NetRpc.Grpc;
using NetRpc.RabbitMQ;
using Proxy.RabbitMQ;
using Helper = TestHelper.Helper;

namespace Client;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var host = new HostBuilder()
            .ConfigureServices((context, services) =>
            {
                services.Configure<GrpcClientOptions>("grpc", i => { i.Url = "http://localhost:50001"; });
                services.AddNGrpcClient();
                services.AddNClientContract<IService>("grpc");
                services.Configure<MQClientOptions>("mq", i => { i.CopyFrom(Helper.GetMQOptions()); });
                services.AddNRabbitMQClient();
                services.AddHostedService<H>();
            })
            .Build();

        await host.RunAsync();
        Console.WriteLine("end");
        Console.Read();
    }
}

public class H : IHostedService
{
    private readonly IService _s1;

    public H(IService s1)
    {
        _s1 = s1;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            var ret = await _s1.Call(new InParam {P1 = "123"}, 100, File.OpenRead(Helper.GetTestFilePath()),
                async i => Console.WriteLine(i), CancellationToken.None);
            Console.WriteLine($"ret:{ret.P1}, {Helper.ReadStr(ret.Stream)}");

            using (var fs = File.OpenWrite(@"d:\1.rar"))
            {
                ret.Stream.CopyTo(fs);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
    }
}