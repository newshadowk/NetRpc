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
using NetRpc.Http.Client;
using NetRpc.RabbitMQ;
using Console = System.Console;
//using NetRpcManager = NetRpc.Grpc.NetRpcManager;
using NetRpcManager = NetRpc.Http.Client.NetRpcManager;

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
                    configApp.SetBasePath(Directory.GetCurrentDirectory());
                    configApp.AddJsonFile("appsettings.json", optional: false);
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddOptions();
                    services.AddHostedService<MyHost>();
                    services.Configure<RabbitMQClientOptions>(context.Configuration.GetSection("Mq1"));
                    services.AddNetRpcRabbitMQClient<IService>();
                })
                .Build();

            await h.StartAsync();
        }
    }

    public class MyHost : IHostedService
    {
        private readonly ClientProxy<IService> _clientProxy;

        public MyHost(ClientProxy<IService> clientProxy)
        {
            _clientProxy = clientProxy;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("start");
            await _clientProxy.Proxy.Call3("33");
            Console.WriteLine("end");
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
        }
    }
}