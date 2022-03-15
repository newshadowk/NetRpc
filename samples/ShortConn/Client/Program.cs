using System;
using System.IO;
using System.Threading.Tasks;
using DataContract;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Client;

internal class Program
{
    private static async Task Main(string[] args)
    {
        Console.WriteLine("\r\n--------------- Client Grpc ---------------");
        var services = new ServiceCollection();
        services.AddNClientContract<IService1Async>();
        //services.AddNGrpcClient(o => o.Url = "http://m.k8s.yx.com:30248");
        services.AddNGrpcClient(o => o.Url = "http://localhost:81");
        var sp = services.BuildServiceProvider();
        await T3(sp);
        Console.WriteLine("\r\n--------------- End ---------------");
        Console.Read();
    }

    private static async Task T1(ServiceProvider sp)
    {
        for (var i = 0; i < 1000; i++)
        {
            using var serviceScope = sp.CreateScope();
            var p = serviceScope.ServiceProvider.GetService<IService1Async>()!;
            await Task.Delay(500);
            try
            {
                await p.Call1Async($"send: {i}");
            }
            catch
            {
                Console.WriteLine($"{i} !");
            }
            Console.WriteLine(i);
        }
    }

    private static async Task T0(ServiceProvider sp)
    {
        var p = sp.GetService<IService1Async>()!;
        for (var i = 0; i < 1000; i++)
        {
            await Task.Delay(500);

            try
            {
                await p.Call1Async($"send: {i}");
            }
            catch
            {
                Console.WriteLine($"{i} !");
            }
            Console.WriteLine(i);
        }
    }

    private static async Task T3(ServiceProvider sp)
    {
        var p = sp.GetService<IService1Async>()!;
        MemoryStream ms = new();
        Console.WriteLine("Call2Async start");
        var r = await p.Call2Async($"send: get stream");
        await r.CopyToAsync(ms);
        Console.WriteLine($"Call2Async end, {ms.Length}");
    }
}