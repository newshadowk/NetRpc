using System;
using DataContract;
using Grpc.Core;

namespace Service
{
    class Program
    {
        static void Main(string[] args)
        {
            var service = NetRpc.Grpc.NetRpcManager.CreateServiceProxy(new ServerPort("0.0.0.0", 50001, ServerCredentials.Insecure), new Service());
            service.Open();
            Console.Read();
        }
    }

    internal class Service : IService
    {
        public void Call(string s)
        {
            Console.WriteLine($"Receive: {s}");
        }
    }
}