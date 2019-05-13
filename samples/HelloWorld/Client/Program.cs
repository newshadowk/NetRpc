using System;
using System.IO;
using DataContract;
using Nrpc;
using Nrpc.Grpc;
using Helper = TestHelper.Helper;
using NRpcManager = Nrpc.RabbitMQ.NRpcManager;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
            //var proxy = NRpcManager.CreateClientProxy<IService>(Helper.GetMQParam()).Proxy;
            var proxy = Nrpc.Grpc.NRpcManager.CreateClientProxy<IService>("localhost", 50001).Proxy;
            proxy.Call("hello world!");
            Console.WriteLine("Send: hello world!");
            Console.Read();
        }
    }
}