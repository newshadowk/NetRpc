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
            var p = NManager.CreateClientProxy<IService>(new GrpcClientOptions
            {
                Host = "lj",
                Port = 50001
            });
            await p.Proxy.Call("hello world.");
            Console.Read();
        }
    }
}