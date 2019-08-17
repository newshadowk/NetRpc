using System;
using System.IO;
using System.Threading.Tasks;
using DataContract;
using NetRpc.RabbitMQ;
using Helper = TestHelper.Helper;

namespace Service
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var host = NetRpcManager.CreateHost(Helper.GetMQOptions(),
                null,
                typeof(ServiceAsync));

            Console.WriteLine("Service Opened.");
            await host.StartAsync();
        }
    }

    internal class ServiceAsync : IService
    {
        public async Task CallAsync(Action<int> cb, string s1)
        {
            Console.WriteLine($"Receive: {s1}, start.");
            for (var i = 0; i < 10; i++)
            {
                await Task.Delay(1000);
                cb(i);
                Console.Write($"{i}, ");
            }

            Console.WriteLine("end.");
        }
    }
}