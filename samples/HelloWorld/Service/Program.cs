using System;
using System.Threading.Tasks;
using DataContract;
using NetRpc.Grpc;

namespace Service
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var o = new GrpcServiceOptions();
            o.AddPort("0.0.0.0", 50001);
            var host = NetRpcManager.CreateHost(o, null, typeof(Service));
            await host.StartAsync();
        }
    }

    internal class Service : IService
    {
        public void Call(string s)
        {
            Console.WriteLine($"Receive: {s}");
        }
    }
}