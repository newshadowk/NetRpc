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

        public H(IService s1)
        {
            _s1 = s1;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            //await _s1.Call()
            try
            {
                await _s1.Call2("123");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}