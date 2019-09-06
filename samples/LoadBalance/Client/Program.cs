using System;
using System.Threading.Tasks;
using DataContract;
using NetRpc.RabbitMQ;
using Helper = TestHelper.Helper;

namespace Client
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var proxy = NetRpcManager.CreateClientProxy<IServiceAsync>(Helper.GetMQOptions()).Proxy;

            for (var i = 0; i < 10; i++)
            {
                var i1 = i;
                Task.Run(async () =>
                {
                    Console.WriteLine($"Send {i1}");
                    await proxy.CallAsync(Console.WriteLine, i1.ToString());
                });
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