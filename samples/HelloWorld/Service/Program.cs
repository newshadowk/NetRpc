using System;
using DataContract;

namespace Service
{
    class Program
    {
        static void Main(string[] args)
        {
            var service = Nrpc.Grpc.NRpcManager.CreateServiceProxy("0.0.0.0", 50001, new Service());
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