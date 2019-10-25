using System;
using System.Threading.Tasks;
using DataContract;
using Grpc.Core;
using NetRpc.Grpc;

namespace Client
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var p = NetRpc.RabbitMQ.NetRpcManager.CreateClientProxy<IService>(TestHelper.Helper.GetMQOptions());
            //var p = NetRpcManager.CreateClientProxy<IService>(new Channel("localhost", 50001, ChannelCredentials.Insecure));
            await p.Proxy.Call("msg");
            Console.Read();
        }
    }
}