using System.Threading;
using System.Threading.Tasks;
using DataContract;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NetRpc;

namespace Client
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddHostedService<GrpcHostedService>();
                    services.AddNetRpcGrpcClient(i =>
                    {
                        i.Host = "localhost";
                        i.Port = 50001;
                    });
                    services.AddNetRpcClientContract<IService>();
                })
                .Build();

            await host.RunAsync();
        }
    }

    public class GrpcHostedService : IHostedService
    {
        private readonly IClientProxy<IService> _client;
        private readonly IService _service;

        public GrpcHostedService(IClientProxy<IService> client, IService service) //DI client here.
        {
            _client = client;
            _service = service;
        }

        public Task StartAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            _service.Call("222");
            _client.Proxy.Call("hello world.");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            return Task.CompletedTask;
        }
    }
}