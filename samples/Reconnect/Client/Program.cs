using System;
using System.Threading;
using System.Threading.Tasks;
using DataContract;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetRpc;
using NetRpc.Grpc;
using NetRpc.RabbitMQ;
using Helper = TestHelper.Helper;

namespace Client
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var h = new HostBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddOptions();
                    services.AddHostedService<MyHost>();
                    services.Configure<RabbitMQClientOptions>("mq", i => i.CopyFrom(Helper.GetMQOptions()));
                    services.Configure<GrpcClientOptions>("grpc", i =>
                    {
                        i.Host = "localhost";
                        i.Port = 50001;
                    });
                    services.AddNetRpcRabbitMQClient();
                    services.AddNetRpcGrpcClient();
                })
                .ConfigureLogging((context, builder) => { builder.AddConsole(); })
                .Build();

            await h.RunAsync();
        }
    }

    public class MyHost : IHostedService
    {
        private readonly IClientProxyFactory _factory;

        public MyHost(IClientProxyFactory factory, ILoggerFactory factory2)
        {
            var l = factory2.CreateLogger("sdfsdf");
            l.LogInformation("test");
            _factory = factory;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var clientProxy = _factory.CreateProxy<IService>("mq");
            clientProxy.Connected += ClientProxy_Connected;
            clientProxy.DisConnected += ClientProxy_DisConnected;
            clientProxy.Heartbeat += ClientProxy_Heartbeat;
            clientProxy.ExceptionInvoked += ClientProxy_ExceptionInvoked;

            Console.WriteLine("start");

#pragma warning disable 4014
            Task.Run(async() =>
#pragma warning restore 4014
            {
                while (true)
                {
                    Console.WriteLine("test start...");
                    try
                    {
                        await clientProxy.Proxy.CallAsync("test");
                        Console.WriteLine("test start...end");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }

                    await Task.Delay(1000);
                }
            });


        }

        private void ClientProxy_ExceptionInvoked(object sender, EventArgsT<Exception> e)
        {
            Console.WriteLine($"ClientProxy_ExceptionInvoked, {e.Value.Message}");
        }

        private Task ClientProxy_Heartbeat(IClientProxy arg)
        {
            Console.WriteLine("ClientProxy_Heartbeat");
            return Task.CompletedTask;
        }

        private void ClientProxy_DisConnected(object sender, EventArgs e)
        {
            Console.WriteLine("ClientProxy_DisConnected");
        }

        private void ClientProxy_Connected(object sender, EventArgs e)
        {
            Console.WriteLine("ClientProxy_Connected");
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("StopAsync");
        }
    }
}