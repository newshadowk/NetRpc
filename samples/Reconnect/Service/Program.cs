using System;
using System.Threading.Tasks;
using DataContract;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetRpc;
using Helper = TestHelper.Helper;

namespace Service
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await RunMQAsync();
        }

        static async Task RunMQAsync()
        {
            var h = new HostBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddNetRpcRabbitMQService(i => { i.CopyFrom(Helper.GetMQOptions()); });
                    //services.AddNetRpcGrpcService(i => i.AddPort("0.0.0.0", 50001));
                    services.AddNetRpcContractSingleton<IService, Service>();
                })
                .ConfigureLogging((context, builder) => { builder.AddConsole(); })
                .Build();
            await h.RunAsync();
        }
    }

    internal class Service : IService
    {
        public Service(ILoggerFactory f)
        {
        }

        public Task CallAsync(string s)
        {
            Console.WriteLine($"CallAsync {s}");
            return Task.CompletedTask;
        }
    }
}