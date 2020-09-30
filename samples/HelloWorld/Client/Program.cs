using System;
using System.Threading.Tasks;
using DataContract;
using NetRpc.Grpc;

namespace Client
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            var p = NManager.CreateClientProxy<IServiceAsync>(new GrpcClientOptions
            {
                Url = "http://localhost:50001"
            });
            await p.Proxy.CallAsync("hello world.");
            Console.Read();
        }
    }
}