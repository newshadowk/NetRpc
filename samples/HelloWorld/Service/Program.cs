using System;
using System.Threading.Tasks;
using DataContract;
using Microsoft.Extensions.Hosting;
using NetRpc;
using NetRpc.Grpc;

namespace Service
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var o = new NGrpcServiceOptions();
            o.AddPort("0.0.0.0", 50001);
            await NManager.CreateHost(o, null, new Contract<IService, Service>()).RunAsync();
        }
    }

    internal class Service : IService
    {
        public async Task Call(string s)
        {
            for (int i = 0; i < 10000; i++)
            {
                Console.WriteLine(i);
                await Task.Delay(1000);
            }
            Console.WriteLine($"Receive: {s}");
        }
    }
}