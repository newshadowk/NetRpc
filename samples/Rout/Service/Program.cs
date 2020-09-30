using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DataContract;
using DataContract1;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NetRpc;
using InParam = DataContract.InParam;
using Ret = DataContract.Ret;

namespace Service
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await RunGrpcAsync();
        }

        static async Task RunGrpcAsync()
        {

            var host = Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureKestrel((context, options) =>
                        {
                            options.ListenAnyIP(50001, listenOptions => listenOptions.Protocols = HttpProtocols.Http2);
                        })
                        .ConfigureServices((context, services) =>
                        {
                            services.AddNGrpcService();
                            services.AddNServiceContract<IService, Service>();
                            services.AddNGrpcClient(i =>
                            {
                                i.Url = "http://localhost:50002";
                            });
                            services.AddNClientContract<IService1>();
                        }).Configure(app =>
                        {
                            app.UseNGrpc();
                        });
                })
                .Build();

            await host.RunAsync();
        }
    }

    internal class Service : IService
    {
        [RouteFilter(typeof(IService1))]
        public async Task<Ret> Call(InParam p, int i, Stream stream, Func<int, Task> progs, CancellationToken token)
        {
            throw new NotImplementedException();
        }
    }
}