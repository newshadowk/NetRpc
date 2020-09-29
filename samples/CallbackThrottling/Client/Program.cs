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
            var p = NManager.CreateClientProxy<IService>(new GrpcClientOptions {Url = "http://localhost:50001" });
            await p.Proxy.Call(async i => Console.WriteLine($"receive callback: {i}"));
            Console.Read();
        }
    }
}