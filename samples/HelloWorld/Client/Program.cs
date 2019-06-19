using System;
using DataContract;
using Grpc.Core;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
            var proxy = NetRpc.Grpc.NetRpcManager.CreateClientProxy<IService>(new Channel("localhost", 50001, ChannelCredentials.Insecure)).Proxy;
            proxy.Call("hello world!");
            proxy.Call(100);

            Console.Read();
        }
    }
}