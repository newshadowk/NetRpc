using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DataContract;
using Grpc.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NetRpc;
using NetRpc.Grpc;
using NetRpc.RabbitMQ;

namespace Client
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var h = new HostBuilder()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .ConfigureAppConfiguration((hostContext, configApp) =>
                {
                    configApp.SetBasePath(AppContext.BaseDirectory);
                    configApp.AddJsonFile("appsettings.json", optional: false);
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddOptions();
                    services.AddHostedService<MyHost>();

                    services.AddNRabbitMQClient();
                    services.Configure<RabbitMQClientOptions>("mq1", context.Configuration.GetSection("Mq1"));
                    services.Configure<RabbitMQClientOptions>("mq2", context.Configuration.GetSection("Mq2"));
                    services.AddNetRpcClientContract<IService>("mq1");

                    services.AddNGrpcClient();
                    services.Configure<GrpcClientOptions>("grpc1", i =>
                    {
                        i.Host = "localhost";
                        i.Port = 50001;
                    });
                    services.Configure<GrpcClientOptions>("grpc2", i =>
                    {
                        i.Host = "localhost";
                        i.Port = 50001;
                    });
                })
                .Build();

            await h.RunAsync();
        }
    }

    public class MyHost : IHostedService
    {
        private readonly IClientProxyFactory _factory;

        public MyHost(IClientProxyFactory factory, IService Iservice)
        {
            _factory = factory;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var clientProxy = _factory.CreateProxy<IService>("grpc1");
            Console.WriteLine("start");
            await clientProxy.Proxy.CallAsync("test");
            Console.WriteLine("end");
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
        }
    }
}