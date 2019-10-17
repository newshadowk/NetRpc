using System;
using System.IO;
using System.Threading.Tasks;
using DataContract;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NetRpc;
using NetRpc.Grpc;
using NetRpc.Http;
using Helper = TestHelper.Helper;

namespace Service
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await RunGrpcAsync();
            //await RunHttpAsync();
        }

        static async Task RunGrpcAsync()
        {
            var o = new GrpcServiceOptions();
            o.AddPort("0.0.0.0", 50001);
            var host = new HostBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddNetRpcGrpcService(i => { i.AddPort("0.0.0.0", 50001); });
                    services.AddNetRpcContractSingleton<IService, Service>();
                    services.AddNetRpcContractSingleton<IService2, Service2>();
                    services.AddNetRpcRabbitMQService(i => i.CopyFrom(Helper.GetMQOptions()));
                })
                .Build();


            await host.RunAsync();
        }

        static async Task RunHttpAsync()
        {
            var host = WebHost.CreateDefaultBuilder(null)
                .ConfigureKestrel(options => { options.ListenAnyIP(5000); })
                .ConfigureServices(services =>
                {
                    services.AddCors();
                    services.AddSignalR();
                    services.AddNetRpcSwagger();
                    services.AddNetRpcHttpService();
                    services.AddNetRpcContractSingleton<IService, Service>();
                })
                .Configure(app =>
                {
                    app.UseCors(i =>
                        {
                            i.AllowAnyHeader();
                            i.AllowAnyMethod();
                            i.AllowCredentials();
                        }
                    );
                    app.UseSignalR(routes => { routes.MapHub<CallbackHub>("/callback"); });
                    app.UseNetRpcSwagger();
                    app.UseNetRpcHttp();
                })
                .Build();
            await host.RunAsync();
        }
    }

    internal class Service : IService
    {
        public Task Call(Stream stream, Action<int> prog)
        {
            const int size = 81920;
            var bs = new byte[size];
            var readCount = stream.Read(bs, 0, size);
            while (readCount > 0)
            {
                readCount = stream.Read(bs, 0, size);
                Console.WriteLine(readCount);
            }

            Console.WriteLine("end");
            return Task.CompletedTask;
        }

        public Task Call2(Stream stream)
        {
            const int size = 81920;
            var bs = new byte[size];
            var readCount = stream.Read(bs, 0, size);
            while (readCount > 0)
            {
                readCount = stream.Read(bs, 0, size);
                Console.WriteLine(readCount);
            }

            Console.WriteLine("end");
            return Task.CompletedTask;
        }

        public async Task<Stream> Call3(Stream stream, Action<int> prog)
        {
            const int size = 81920;
            var bs = new byte[size];
            var readCount = stream.Read(bs, 0, size);
            while (readCount > 0)
            {
                readCount = stream.Read(bs, 0, size);
                Console.WriteLine(readCount);
            }

            Console.WriteLine("end");
            var fileStream = File.OpenRead(@"d:\7\4-.rar");
            return fileStream;
        }

        public async Task Call4()
        {
            Console.WriteLine("s1 call");
        }
    }

    internal class Service2 : IService2
    {
        public async Task Call()
        {
            Console.WriteLine("s2 call");
        }
    }
}