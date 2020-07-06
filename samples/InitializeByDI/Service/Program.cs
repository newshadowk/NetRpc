using System;
using System.Threading.Tasks;
using DataContract;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NetRpc;
using NetRpc.Grpc;

namespace Service
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddNGrpcService(i => i.AddPort("0.0.0.0", 50001));
                    services.AddNRpcServiceContract<IService, Service>();
                })
                .Build();

            await host.RunAsync();
        }
    }

    internal class Service : IService
    {
        public async Task Call(string s)
        {
            Console.WriteLine($"Receive: {s}");
        }
    }
}