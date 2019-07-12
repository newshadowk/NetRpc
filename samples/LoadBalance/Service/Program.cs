using System;
using System.IO;
using System.Threading.Tasks;
using DataContract;

namespace Service
{
    class Program
    {
        static void Main(string[] args)
        {
            TestHelper.Helper.OpenRabbitMQService(new Service());
            Console.WriteLine("Service Opened.");
            Console.Read();
        }
    }

    internal class Service : IService
    {
        //public async Task CallAsync(string s)
        //{
        //    Console.WriteLine($"Receive: {s}, start.");
        //    for (int i = 0; i < 10; i++)
        //    {
        //        await Task.Delay(1000);
        //        Console.Write($"{i}, ");
        //    }
        //    Console.WriteLine($"end.");
        //}

        public async Task CallAsync(Stream s, Action<int> cb, string s1)
        {
            Console.WriteLine($"Receive: {s1}, start.");
            for (int i = 0; i < 10; i++)
            {
                await Task.Delay(1000);
                cb(i);
                Console.Write($"{i}, ");
            }
            Console.WriteLine($"end.");
        }
    }
}