using System;
using System.Net;
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
            //dns解析
            var ipHostEntry = await Dns.GetHostEntryAsync("www.contoso.com");

            //register
            var services = new ServiceCollection();
            services.AddNGrpcClient(options => options.Url = "http://localhost:50001");
            services.AddNClientContract<IServiceAsync>();
            var sp = services.BuildServiceProvider();

            //get service
            var service = sp.GetService<IServiceAsync>();
            Console.WriteLine("call: hello world.");
            var ret = await service.CallAsync("hello world.");
            Console.WriteLine($"ret: {ret}");
            Console.Read();
        }
    }
}