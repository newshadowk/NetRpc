using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DataContract;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Helper = TestHelper.Helper;

namespace Service
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddNetRpcGrpcService(i => { i.AddPort("0.0.0.0", 50001); });

                    services.AddNetRpcServiceContract<IService, Service>();
                    services.AddNetRpcServiceContract<IService2, Service2>();
                })
                .Build();
            await host.RunAsync();
        }
    }

    internal class Service2 : IService2
    {
        public async Task Call2(string s)
        {
            Console.WriteLine($"Receive: {s}");
        }
    }

    internal class Service : IService
    {
        public async Task Call(string s)
        {
            Console.WriteLine($"Receive: {s}");
        }

        public async Task<ComplexStream> ComplexCallAsync(CustomObj obj, Stream data, Func<CustomCallbackObj, Task> cb, CancellationToken token)
        {
            Console.Write($"[ComplexCallAsync]...Received length:{data.Length}, {Helper.ReadStr(data)}, ");
            for (var i = 1; i <= 3; i++)
            {
                Console.Write($"{i}, ");
                await cb(new CustomCallbackObj {Progress = i});
                await Task.Delay(100, token);
            }

            Console.WriteLine("...Send TestFile.txt");
            return new ComplexStream
            {
                Stream = File.Open(Helper.GetTestFilePath(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite),
                OtherInfo = "this is other info"
            };
        }
    }
}