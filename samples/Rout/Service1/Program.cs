using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DataContract1;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NetRpc.Grpc;
using TestHelper;

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
                    services.AddNetRpcGrpcService(i => { i.AddPort("0.0.0.0", 50002); });
                    services.AddNetRpcServiceContract<IService1, Service1>();
                })
                .Build();

            await host.RunAsync();
        }
    }

    internal class Service1 : IService1
    {
        public async Task<Ret> Call(InParam p, int i, Stream stream, Func<int, Task> progs, CancellationToken token)
        {
            Console.WriteLine($"{p}, {i}, {Helper.ReadStr(stream)}");

            for (int i1 = 0; i1 < 3; i1++)
            {
                await progs(i1);
                await Task.Delay(100, token);
            }

            return new Ret
            {
                Stream = File.OpenRead(Helper.GetTestFilePath()),
                P1 = "return p1"
            };
        }
    }
}