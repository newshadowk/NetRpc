using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DataContract;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NetRpc;
using Helper = TestHelper.Helper;

namespace GrpcService
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureKestrel((_, options) => { options.ListenAnyIP(50001, listenOptions => listenOptions.Protocols = HttpProtocols.Http2); })
                        .ConfigureServices((_, services) =>
                        {
                            services.AddNGrpcService();
                            services.AddNServiceContract<IServiceAsync, ServiceAsync>();
                            services.AddNServiceContract<IService2Async, Service2Async>();
                        }).Configure(app => { app.UseNGrpc(); });
                }).Build();

            await host.RunAsync();
        }
    }

    internal class Service2Async : IService2Async
    {
        public async Task Call2(string s)
        {
            Console.WriteLine($"Receive: {s}");
        }
    }

    internal class ServiceAsync : IServiceAsync
    {
        public async Task Call(string s)
        {
            var h = GlobalActionExecutingContext.Context.Header;
            Console.WriteLine($"Receive: {s}, {h["k1"]}");
        }

        public async Task<ComplexStream> ComplexCallAsync(CustomObj obj, Stream data, Func<CustomCallbackObj, Task> cb, CancellationToken token)
        {
            Console.Write($"[ComplexCallAsync]...Received length:{data.Length}, {Helper.ReadStr(data)}, ");
            for (var i = 1; i <= 3; i++)
            {
                Console.Write($"{i}, ");
                await cb(new CustomCallbackObj { Progress = i });
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