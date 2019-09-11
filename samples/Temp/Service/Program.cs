using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DataContract;
using Microsoft.Extensions.Hosting;
using NetRpc;
using NetRpc.Grpc;
using NetRpc.RabbitMQ;
using Helper = TestHelper.Helper;

namespace Service
{
    class Program
    {
        static async Task Main(string[] args)
        {
            //ThreadPool.SetMinThreads(110, 110);
            //for (int i = 0; i < 1000; i++)
            //{
            //    //Task.Factory.StartNew(() => { });
            //    Task.Run(() =>
            //    {
            //        Console.WriteLine($"{Thread.CurrentThread.ManagedThreadId}, {DateTime.Now}");
            //        Thread.Sleep(1000000);
            //    });
            //}

            //Console.Read();

            await RunMQAsync();
        }

        static async Task RunMQAsync()
        {
            var h = new HostBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddNetRpcRabbitMQService(i =>
                    {
                        i.Value = Helper.GetMQOptions();
                        i.Value.PrefetchCount = 5;
                    });
                    services.AddNetRpcGrpcService(i => i.AddPort("0.0.0.0", 50001));

                    services.AddNetRpcMiddleware(i =>
                    {
                        i.UseMiddleware<CallbackThrottlingMiddleware>(500);
                        i.UseMiddleware<StreamCallBackMiddleware>(10);
                        i.UseMiddleware<ExMiddleware>();
                    });
                    services.AddNetRpcContractSingleton<IService, Service>();
                })
                .Build();
            await h.RunAsync();
        }
    }

    internal class Service : IService
    {
        public async Task Call3(Stream stream, int index, Action<double> prog)
        {
            Console.WriteLine($"id:{Thread.CurrentThread.ManagedThreadId}, index:{index}, start");

            using (var os = File.OpenWrite($@"d:\7\test\tgt\{index}.zip"))
            {
                await stream.CopyToAsync(os);
            }

            //const int size = 81920;
            //var bs = new byte[size];
            //var readCount = await stream.ReadAsync(bs, 0, size);
            //Console.WriteLine($"{index}, {readCount}");

            //while (readCount > 0)
            //{
            //    readCount = await stream.ReadAsync(bs, 0, size);
            //    Console.WriteLine($"id:{Thread.CurrentThread.ManagedThreadId}, index:{index}, {readCount}");
            //}

            Console.WriteLine($"id:{Thread.CurrentThread.ManagedThreadId}, index:{index}, end ------------------------");
        }
    }

    public class ExMiddleware
    {
        private readonly RequestDelegate _next;

        public ExMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(RpcContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}