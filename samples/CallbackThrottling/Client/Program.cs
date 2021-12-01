using System;
using System.Threading.Tasks;
using DataContract;
using Microsoft.Extensions.DependencyInjection;

namespace Client;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var services = new ServiceCollection();
        services.AddNGrpcClient(options => options.Url = "http://localhost:50001");
        services.AddNClientContract<IServiceAsync>();
        services.AddLogging();
        var sp = services.BuildServiceProvider();
        var p = sp.GetService<IServiceAsync>();
        await p.CallAsync(async i => Console.WriteLine($"receive callback: {i}"));
        Console.Read();
    }
}