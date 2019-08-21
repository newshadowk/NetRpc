using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using DataContract;
using NetRpc.RabbitMQ;
using RabbitMQ.Client;
using Helper = TestHelper.Helper;

namespace Client
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var proxy = NetRpcManager.CreateClientProxy<IService>(Helper.GetMQOptions()).Proxy;
            for (int i = 0; i < 10; i++)
            {
                await proxy.CallAsync(Console.WriteLine, i.ToString());
                Console.WriteLine($"Send {i}");
            }

            //for (int i = 0; i < 10; i++)
            //{
            //    using (var stream = File.Open(Helper.GetTestFilePath(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            //        await proxy.PostAsync(i.ToString(), stream);
            //    Console.WriteLine($"post {i}");
            //}

            Console.WriteLine("Send end");
            Console.Read();
        }
    }
}
