using System;
using System.Threading.Tasks;
using DataContract;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NetRpc;
using Helper = TestHelper.Helper;

namespace Service
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await RunAsync();
        }

        static async Task RunAsync()
        {
            var h = new HostBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddNRabbitMQService(i => { i.CopyFrom(Helper.GetMQOptions()); });
                    services.AddNGrpcService(i => i.AddPort("0.0.0.0", 50001));
                    services.AddNRpcServiceContract<IService, Service>();
                })
                .Build();
            await h.RunAsync();
        }
    }

    internal class Service : IService
    {
        public Task CallAsync(string s)
        {
            Console.WriteLine($"CallAsync {s}");
            return Task.CompletedTask;
        }
    }
}