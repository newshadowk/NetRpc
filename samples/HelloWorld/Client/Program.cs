using System;
using System.IO;
using System.Threading.Tasks;
using DataContract;
using Grpc.Core;
using NetRpc;
using NetRpc.Grpc;

namespace Client
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var p = NetRpcManager.CreateClientProxy<IService>(new Channel("localhost", 50001, ChannelCredentials.Insecure));
            await p.Proxy.Call("hello world.");
            Console.Read();
        }
    }
}