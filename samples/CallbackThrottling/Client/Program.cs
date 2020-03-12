using System;
using System.Threading.Tasks;
using DataContract;
using NetRpc.Grpc;

namespace Client
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var p = NetRpcManager.CreateClientProxy<IService>(new GrpcClientOptions {Host = "localhost", Port = 50001});
            await p.Proxy.Call(async i => Console.WriteLine($"receive callback: {i}"));
            Console.Read();
        }
    }
}