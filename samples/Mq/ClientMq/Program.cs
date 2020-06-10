using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using RabbitMQ.Base;
using RabbitMQ.Client;
using TestHelper;

namespace ClientMq
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var options = Helper.GetMQOptions();

            var c = new ConnectionFactory
            {
                UserName = options.User,
                Password = options.Password,
                VirtualHost = options.VirtualHost,
                HostName = options.Host,
                Port = options.Port,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(5),
                DispatchConsumersAsync = true
            }.CreateConnection();

            for (int i = 0; i < 10000; i++)
            {
                try
                {
                    await Task.Delay(1000);
                    using var call = new RabbitMQOnceCall(c, "testQ", NullLogger.Instance);
                    Console.WriteLine($"CreateChannelAsync {i}");
                    await call.CreateChannelAsync();
                    Console.WriteLine($"send cmd {i}");
                    await call.SendAsync(Encoding.UTF8.GetBytes("cmd"), false);
                    Console.WriteLine("send body");
                    await call.SendAsync(Encoding.UTF8.GetBytes("body"), false);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }

            Console.WriteLine("end");
            Console.Read();
        }
    }
}
