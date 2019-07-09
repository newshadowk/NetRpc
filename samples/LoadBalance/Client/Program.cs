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
            var proxy = NetRpcManager.CreateClientProxy<IService>(Helper.GetMQParam()).Proxy;
            var fileStream = File.Open(@"d:\4\1212.pdf", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            for (int i = 0; i < 10; i++)
            {
                proxy.CallAsync(fileStream, Console.WriteLine, i.ToString());
                Console.WriteLine($"Send {i}");
            }

            Console.WriteLine($"Send end");
            Console.Read();
        }
    }
}
