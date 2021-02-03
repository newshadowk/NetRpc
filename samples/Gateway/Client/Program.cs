using System;
using System.IO;
using System.Threading.Tasks;
using DataContract;
using Microsoft.Extensions.DependencyInjection;
using NetRpc;
using Helper = TestHelper.Helper;

namespace Client
{
    internal class Program
    {
        private static IClientProxy<IServiceAsync> _c1;
        private static IClientProxy<IService2Async> _c2;

        private static async Task Main(string[] args)
        {
            await RabbitMQ();
            await Grpc();
            await Http();

            Console.WriteLine("\r\n--------------- End ---------------");
            Console.Read();
        }

        private static async Task RabbitMQ()
        {
            //RabbitMQ
            Console.WriteLine("\r\n--------------- Client RabbitMQ ---------------");
            var services = new ServiceCollection();
            services.AddNClientContract<IServiceAsync>();
            services.AddNClientContract<IService2Async>();
            services.AddNRabbitMQClient(o => o.CopyFrom(Helper.GetMQOptions()));
            var sp = services.BuildServiceProvider();
            _c1 = sp.GetService<IClientProxy<IServiceAsync>>();
            _c2 = sp.GetService<IClientProxy<IService2Async>>();
            await TestAsync();
        }

        private static async Task Grpc()
        {
            Console.WriteLine("\r\n--------------- Client Grpc ---------------");
            var services = new ServiceCollection();
            services.AddNClientContract<IServiceAsync>();
            services.AddNClientContract<IService2Async>();
            services.AddNGrpcClient(o => o.Url = "http://localhost:50000");
            var sp = services.BuildServiceProvider();
            _c1 = sp.GetService<IClientProxy<IServiceAsync>>();
            _c2 = sp.GetService<IClientProxy<IService2Async>>();
            await TestAsync();
        }

        private static async Task Http()
        {
            Console.WriteLine("\r\n--------------- Client Http ---------------");
            var services = new ServiceCollection();
            services.AddNClientContract<IServiceAsync>();
            services.AddNClientContract<IService2Async>();
            services.AddNHttpClient(o =>
            {
                o.SignalRHubUrl = "http://localhost:5000/callback";
                o.ApiUrl = "http://localhost:5000/api";
            });
            var sp = services.BuildServiceProvider();
            _c1 = sp.GetService<IClientProxy<IServiceAsync>>();
            _c2 = sp.GetService<IClientProxy<IService2Async>>();
            await TestAsync();
        }

        private static async Task TestAsync()
        {
            await Test_Call();
            await Test_Call2();
            await Test_ComplexCallAsync();
        }

        private static async Task Test_Call()
        {
            Console.Write("[Call]...Send 123...");
            _c1.AdditionHeader.Add("k1", "k1 value");
            await _c1.Proxy.Call("123");
            Console.WriteLine("end.");
        }

        private static async Task Test_Call2()
        {
            Console.Write("[Call2]...Send 123...");
            await _c2.Proxy.Call2("123");
            Console.WriteLine("end.");
        }

        private static async Task Test_ComplexCallAsync()
        {
            using (var stream = File.Open(Helper.GetTestFilePath(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                Console.Write("[ComplexCallAsync]...Send TestFile.txt...");
                var complexStream = await _c1.Proxy.ComplexCallAsync(
                    new CustomObj {Date = DateTime.Now, Name = "ComplexCall"},
                    stream,
                    async i => Console.Write(", " + i.Progress),
                    default);

                using (var stream2 = complexStream.Stream)
                    Console.Write($", receive length:{stream.Length}, {Helper.ReadStr(stream2)}");
                Console.WriteLine($", otherInfo:{complexStream.OtherInfo}");
            }
        }
    }
}