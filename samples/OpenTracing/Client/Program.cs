using System;
using System.IO;
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
            await pp.Call("msg");

            //Console.WriteLine("send 400MB...");
            //using var r = File.OpenRead(@"d:\testfile\400MB.exe");
            //var retS = await pp.Echo(r);
            //Console.WriteLine($"revi:{retS.Length}");


            Console.WriteLine("--- end ---");
            Console.Read();
        }
    }
}