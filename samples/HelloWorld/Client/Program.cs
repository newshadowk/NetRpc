using System;
using DataContract;
using Grpc.Core;
using NetRpc.Grpc;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
            var p = NetRpcManager.CreateClientProxy<IService>(new Channel("localhost", 50001, ChannelCredentials.Insecure));
            p.Proxy.Call("hello world.");
            Console.Read();
        }
    }
}