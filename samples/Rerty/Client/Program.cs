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
        var services = new ServiceCollection();
        services.AddNClientContract<IServiceAsync>();
        services.AddNGrpcClient(o => o.Url = "http://localhost:50001");
        services.AddLogging(b => b.AddConsole());
        var sp = services.BuildServiceProvider();
        var p = sp.GetService<IServiceAsync>();

        await p.CallAsync("hello world.");

        //await using (var fr = File.OpenRead(TestHelper.Helper.GetTestFilePath()))
        //{
        //    await p.Call2Async(fr);
        //}

        Console.Read();
    }
}