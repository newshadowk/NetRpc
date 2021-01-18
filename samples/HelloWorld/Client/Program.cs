using System;
using System.Threading.Tasks;
using DataContract;
using Microsoft.Extensions.DependencyInjection;
using NetRpc;
using NetRpc.Grpc;

namespace Client
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            //register
            var services = new ServiceCollection();
            services.AddNGrpcClient(options => options.Url = "http://localhost:50001");
            services.AddNClientContract<IServiceAsync>();
            services.AddLogging();
            var buildServiceProvider = services.BuildServiceProvider();

            //get service
            var service = buildServiceProvider.GetService<IServiceAsync>();
            Console.WriteLine("call: hello world.");
            var ret = await service.CallAsync("hello world.");
            Console.WriteLine($"ret: {ret}");
            Console.Read();
        }
    }
}