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
                    services.AddNetRpcGrpcService(i => i.AddPort("0.0.0.0", 50001));
                    services.AddNetRpcServiceContract<Service>();
                })
                .Build();

            await host.StartAsync();
        }
    }

    internal class Service : IService
    {
        public Task Call2(string s, Action<int> cb)
        {
            throw new NotImplementedException();
        }

        public async Task Call(string s)
        {
            Console.WriteLine($"Receive: {s}");
        }
    }
}