using System;
using System.Threading.Tasks;
using DataContract;
using Microsoft.Extensions.DependencyInjection;

namespace Client
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            var services = new ServiceCollection();
            services.AddNClientContract<IService>();
            services.AddNGrpcClient(o => o.Url = "http://localhost:50001");
            var sp = services.BuildServiceProvider();
            var pp = sp.GetService<IService>();
            //var ret = await pp.Proxy.Call_1(new SendObj(), 1, false, null, default);
            await pp.Call("msg");

            Console.WriteLine("--- end ---");
            Console.Read();
        }
    }
}