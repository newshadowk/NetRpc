using System;
using DataContract;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
            var proxy = NetRpc.Grpc.NetRpcManager.CreateClientProxy<IService>("localhost", 50001).Proxy;
            proxy.Call("hello world!");
            Console.Read();
        }
    }
}