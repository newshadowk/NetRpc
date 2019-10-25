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
                    services.AddNetRpcGrpcService(i => { i.AddPort("0.0.0.0", 50001); });
                    services.AddNetRpcContractSingleton<IService, Service>();
                    services.AddNetRpcGrpcClient(i => i.Channel = new Channel("localhost", 50002, ChannelCredentials.Insecure));
                    services.AddNetRpcClientContract<IService1>();
                })
                .Build();

            await host.RunAsync();
        }
    }

    internal class Service : IService
    {
        [RouteFilter(typeof(IService1))]
        public async Task<Ret> Call(InParam p, int i, Stream stream, Action<int> progs, CancellationToken token)
        {
            throw new NotImplementedException();
        }
    }
}