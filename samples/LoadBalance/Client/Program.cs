using System;
using System.Threading.Tasks;
using DataContract;
using Microsoft.Extensions.DependencyInjection;
using TestHelper;

namespace Client;

internal class Program
{
    private static async Task Main(string[] args)
    {
        ServiceProvider sp;
        var services = new ServiceCollection();
        services.AddNClientContract<IServiceAsync>();
        services.AddNRabbitMQClient(o => o.CopyFrom(Helper.GetMQOptions()));
        sp = services.BuildServiceProvider();
        var proxy = sp.GetService<IServiceAsync>();

        for (var i = 0; i < 10; i++)
        {
            var i1 = i;
            Task.Run(async () =>
            {
                Console.WriteLine($"Send {i1}");
                await proxy.CallAsync(async p => Console.WriteLine(p), i1.ToString());
            });
        }

        //for (int i = 0; i < 10; i++)
        //{
        //    using (var stream = File.Open(Helper.GetTestFilePath(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        //        await proxy.PostAsync(i.ToString(), stream);
        //    Console.WriteLine($"post {i}");
        //}

        Console.WriteLine("Send end");
        Console.Read();
    }
}