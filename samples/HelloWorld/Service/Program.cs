using System;
using System.IO;
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
            await NManager.CreateHost(o, null, new ContractParam<IService, Service>()).RunAsync();
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