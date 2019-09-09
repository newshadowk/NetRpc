using System;
using System.IO;
using System.Threading.Tasks;
using DataContract;
using Microsoft.Extensions.Hosting;
using NetRpc;
using NetRpc.Grpc;

namespace Service
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var o = new GrpcServiceOptions();
            o.AddPort("0.0.0.0", 50001);
            var host = new HostBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddNetRpcGrpcService(i =>
                    {
                        i.AddPort("0.0.0.0", 50001);
                    });
                    services.AddNetRpcStreamCallBack(10);
                    services.AddNetRpcContractSingleton<IService, Service>();
                })
                .Build();
            await host.RunAsync();
        }
    }

    internal class Service : IService
    {
        public Task Call(Stream stream, Action<int> prog)
        {
            const int size = 81920;
            var bs = new byte[size];
            var readCount = stream.Read(bs, 0, size);
            while (readCount > 0)
            {
                readCount = stream.Read(bs, 0, size);
                Console.WriteLine(readCount);
            }

            Console.WriteLine("end");
            return Task.CompletedTask;
        }

        public Task Call2(Stream stream)
        {
            const int size = 81920;
            var bs = new byte[size];
            var readCount = stream.Read(bs, 0, size);
            while (readCount > 0)
            {
                readCount = stream.Read(bs, 0, size);
                Console.WriteLine(readCount);
            }

            Console.WriteLine("end");
            return Task.CompletedTask;
        }
    }
}