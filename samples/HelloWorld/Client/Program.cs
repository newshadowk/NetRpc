using System;
using System.Text;
using System.Threading.Tasks;
using DataContract;
// ReSharper disable once RedundantUsingDirective
using Google.Protobuf;
using Grpc.Net.Client;
using NetRpc.Grpc;
using Proxy.Grpc;

namespace Client
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var p = NManager.CreateClientProxy<IService>(new GrpcClientOptions
            {
                Url = "http://localhost:50001"
            });
            await p.Proxy.Call("hello world.");
            Console.Read();
        }
    }
}