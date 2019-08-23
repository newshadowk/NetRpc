using System;
using System.Threading;
using System.Threading.Tasks;
using DataContract;
using Microsoft.Extensions.Hosting;
using NetRpc;
using NetRpc.Grpc;
using NetRpc.RabbitMQ;
using Helper = TestHelper.Helper;

namespace Service
{
    class Program
    {
        static async Task Main(string[] args)
        {
            //var o = new GrpcServiceOptions();
            //o.AddPort("0.0.0.0", 50001);
            var host =  new HostBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddNetRpcGrpcService(i =>
                        {
                                i.AddPort("0.0.0.0", 50001);
                        });
                    services.AddNetRpcServiceContract(typeof(Service));
                    services.AddNetRpcRabbitMQService(o => o.Value = Helper.GetMQOptions());
                    services.AddCallbackThrottling(500);
                })
                .Build();

            await host.StartAsync();
        }
    }

    internal class Service : IService
    {
        public async Task Call(string s)
        {
            Console.WriteLine($"Receive: {s}");
        }

        public async Task Call2(string s, Action<int> cb)
        {
            Console.WriteLine($"Receive: {s}");
            for (int i = 0; i < 5; i++)
            {
                cb(i);
            }
        }
    }
}