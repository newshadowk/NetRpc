using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using DataContract;
using Nrpc;
using Helper = TestHelper.Helper;

namespace Service
{
    class Program
    {
        static void Main(string[] args)
        {
            //Helper.OpenRabbitMQService(new Service());
            var s = Helper.OpenGrpcService(new Service());
            Console.WriteLine("Service Opened.");
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