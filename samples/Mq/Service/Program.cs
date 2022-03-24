using System;
using System.Threading.Tasks;
using DataContract;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Helper = TestHelper.Helper;

namespace Service;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var mpHost = new HostBuilder()
            .ConfigureServices((_, services) =>
            {
                services.AddNRabbitMQService(i => i.CopyFrom(Helper.GetMQOptions()));
                services.AddNServiceContract<IServiceAsync, ServiceAsync>();
            }).ConfigureLogging((_, loggingBuilder) =>
            {
                loggingBuilder.AddConsole();
            })
            .Build();
        await mpHost.RunAsync();

        Console.Read();
    }
}