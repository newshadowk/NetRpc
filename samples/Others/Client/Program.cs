using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DataContract1;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NetRpc.Grpc;
using NetRpc.RabbitMQ;
using Helper = TestHelper.Helper;

//using NManager = NetRpc.Grpc.NManager;

namespace Client
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.Configure<GrpcClientOptions>("grpc", i =>
                    {
                        i.Host = "localhost";
                        i.Port = 50002;
                    });
                    services.AddNGrpcClient();
                    services.AddNetRpcClientContract<IService1>("grpc");

                    services.Configure<RabbitMQClientOptions>("mq", i => { i.CopyFrom(Helper.GetMQOptions()); });

                    services.AddNRabbitMQClient();

                    services.AddHostedService<H>();
                })
                .Build();

            await host.RunAsync();
            Console.WriteLine("end");
            Console.Read();
        }
    }

    public class H : IHostedService
    {
        private readonly IService1 _s1;

        public H(IService1 s1)
        {
            _s1 = s1;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                Console.WriteLine("start");
                var sw = new Stopwatch();
                sw.Start();
                using (var fr = File.OpenRead(@"d:\testfile\testfile.txt"))
                {
                    var ret = await _s1.Call(
                        new InParam {P1 = "123"}, 100,
                        //File.OpenRead(Helper.GetTestFilePath()),
                        fr,
                        async i => Console.WriteLine(i),
                        CancellationToken.None);
                    Console.WriteLine($"ret:{ret.P1}");

                    using (var fs = File.OpenWrite(@"D:\TestFile\tgt.rar"))
                    {
                        ret.Stream.CopyTo(fs);
                    }
                }

                sw.Stop();
                Console.WriteLine(sw.ElapsedMilliseconds);

                //Console.WriteLine($"ret:{ret.P1}, {Helper.ReadStr(ret.Stream)}");

                //await _s1.Call2("123");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
        }
    }
}