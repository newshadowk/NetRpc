using System;
using System.IO;
using System.Threading.Tasks;
using DataContract;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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

        var service = sp.GetService<IServiceAsync>();
        var fs = File.OpenRead(@"D:\TestFile\10MB.db");

        try
        {
            Console.WriteLine("send start");
            // var r = await service.Call2Async(fs);
            // Console.WriteLine(r);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
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