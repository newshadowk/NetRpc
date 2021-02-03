using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DataContract1;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Helper = TestHelper.Helper;

namespace Service
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureKestrel((_, options) =>
                        {
                            options.ListenAnyIP(50001, listenOptions => listenOptions.Protocols = HttpProtocols.Http2);
                        })
                        .ConfigureServices((_, services) =>
                        {
                            services.AddNGrpcService();
                            services.AddNServiceContract<IService1, Service1>();
                        }).Configure(app => { app.UseNGrpc(); });
                }).Build();
            await host.RunAsync();
        }
    }

    internal class Service1 : IService1
    {
        public async Task<Ret> Call(InParam p, int i, Stream stream, Func<int, Task> progs, CancellationToken token)
        {
            Console.WriteLine($"{p}, {i}, {Helper.ReadStr(stream)}");

            for (var i1 = 0; i1 < 3; i1++)
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