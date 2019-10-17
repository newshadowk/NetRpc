using System;
using System.Threading;
using System.Threading.Tasks;
using DataContract;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NetRpc.Grpc;
using NetRpc.RabbitMQ;
using Helper = TestHelper.Helper;
//using NetRpcManager = NetRpc.Grpc.NetRpcManager;

namespace Client
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.Configure<GrpcClientOptions>("grpc", i => i.Channel = new Channel("localhost", 50001, ChannelCredentials.Insecure));
                    services.AddNetRpcGrpcClient();
                    services.AddNetRpcClientContract<IService>("grpc");

                    services.Configure<RabbitMQClientOptions>("mq", i =>
                    {
                        i.CopyFrom(Helper.GetMQOptions());
                    });

                    services.AddNetRpcRabbitMQClient();
                    services.AddNetRpcClientContract<IService2>("mq");

                    services.AddHostedService<H>();
                })
                .Build();

            await host.RunAsync();
            Console.WriteLine("end");
            Console.Read();
        }
    }

    public class H : IHostedService
    {
        private readonly IService _s1;
        private readonly IService2 _s2;

        public H(IService s1, IService2 s2)
        {
            _s1 = s1;
            _s2 = s2;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _s1.Call4();
            await _s2.Call();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}