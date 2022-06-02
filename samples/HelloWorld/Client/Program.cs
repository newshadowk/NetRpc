using System;
using System.Threading.Tasks;
using DataContract;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetRpc;

namespace Client;

internal class Program
{
    private static async Task Main(string[] args)
    {
        //register
        var services = new ServiceCollection();

        services.AddNGrpcClient(options => { options.Url = "http://localhost:50001"; });
        services.AddNClientContract<IServiceAsync>();
        services.AddLogging(l => l.AddConsole());
        var sp = services.BuildServiceProvider();

        //get service
        var service = sp.GetService<IServiceAsync>();
        Console.WriteLine("call: hello world.");
        try
        {
            var ret = await service.CallAsync("hello world.");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        Console.Read();
    }
}