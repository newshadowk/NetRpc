using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DataContract;
using DataContract1;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NetRpc;
using InParam = DataContract.InParam;
using Ret = DataContract.Ret;

namespace Service
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await RunGrpcAsync();
        }

        static async Task RunGrpcAsync()
        {
            var host = new HostBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddNGrpcService(i => { i.AddPort("0.0.0.0", 50001); });
                    services.AddNRpcServiceContract<IService, Service>();
                    services.AddNGrpcClient(i =>
                    {
                        i.Host = "localhost";
                        i.Port = 50002;
                    });
                    services.AddNRpcClientContract<IService1>();
                })
                .Build();

            await host.RunAsync();
        }
    }

    internal class Service : IService
    {
        [RouteFilter(typeof(IService1))]
        public async Task<Ret> Call(InParam p, int i, Stream stream, Func<int, Task> progs, CancellationToken token)
        {
            throw new NotImplementedException();
        }
    }
}