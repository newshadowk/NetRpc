using System;
using System.IO;
using DataContract;
using NetRpc.RabbitMQ;
using Helper = TestHelper.Helper;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
            var proxy = NetRpcManager.CreateClientProxy<IService>(Helper.GetMQOptions()).Proxy;
            for (int i = 0; i < 10; i++)
            {
                proxy.CallAsync(Console.WriteLine, i.ToString());
                Console.WriteLine($"Send {i}");
            }

            Console.WriteLine("Send end");
            Console.Read();
        }
    }
}
