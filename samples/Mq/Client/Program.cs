using System;
using System.IO;
using System.Threading.Tasks;
using DataContract;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetRpc;
using Helper = TestHelper.Helper;

namespace Client;

internal class Program
{
    private static IServiceAsync _proxyAsync;

    private static async Task Main(string[] args)
    {
        var services = new ServiceCollection();
        services.AddNClientContract<IServiceAsync>();
        services.AddLogging(configure => configure.AddConsole());
        services.AddNRabbitMQClient(o => o.CopyFrom(Helper.GetMQOptions()));
        var sp = services.BuildServiceProvider();
        _proxyAsync = sp.GetService<IClientProxy<IServiceAsync>>()!.Proxy;
        //var r = await _proxyAsync.Call2("123");
        try
        {
            await Test_ComplexCallAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        //await Test_ComplexCallAsync();

        //DoT();

        Console.WriteLine("\r\n--------------- End ---------------");
        Console.Read();
    }

    private static async Task DoT()
    {
        while (true)
        {
            await Test_ComplexCallAsync();
            //await Test_Call2();
            GC.Collect();
        }
    }

    private static async Task Test_Call2()
    {
        Console.WriteLine("call2 start");
        await _proxyAsync.Call2("123");
        Console.WriteLine("call2 end");
    }

    private static async Task Test_ComplexCallAsync()
    {
        using (var stream = File.Open(Helper.GetTestFilePath(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            Console.Write("[ComplexCallAsync]...Send TestFile.txt...");
            var complexStream = await _proxyAsync.ComplexCallAsync(
                new CustomObj { Date = DateTime.Now, Name = "ComplexCall" },
                stream,
                async i => Console.Write(", " + i.Progress),
                default);

            using (var stream2 = complexStream.Stream)
                Console.Write($", receive length:{stream.Length}");
            Console.WriteLine($", otherInfo:{complexStream.OtherInfo}");
        }
    }
}