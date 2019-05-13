using System;
using System.Collections.Generic;
using DataContract;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
            var proxy = Nrpc.Grpc.NRpcManager.CreateClientProxy<IService>("localhost", 50001).Proxy;
            proxy.Call("hello world!");
            Console.Read();
        }
    }
}