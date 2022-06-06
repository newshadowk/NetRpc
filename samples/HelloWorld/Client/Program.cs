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

        for (int i = 0; i < 5; i++)
        {
            int j = i;
            Task.Run(async () =>
            {
                int i0 = 0;
                while (true)
                {
                    using var c = sp.CreateScope();
                    var s = c.ServiceProvider.GetService<IServiceAsync>();
                    await s.CallAsync($"{i0++}");
                    Console.WriteLine($"{j} - {i0}");
                }
            });
        }
        

        //get service
        //var service = sp.GetService<IServiceAsync>();
        //Console.WriteLine("call: hello world.");
        //try
        //{
        //    var ret = await service.CallAsync("hello world.");
        //}
        //catch (Exception e)
        //{
        //    Console.WriteLine(e);
        //    throw;
        //}

        Console.Read();
    }
}