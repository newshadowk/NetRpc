using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DataContract;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetRpc;
using Helper = TestHelper.Helper;

namespace Service
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await RunMQAsync();
        }

        static async Task RunMQAsync()
        {
            var h = new HostBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddNetRpcRabbitMQService(i =>
                    {
                       i.CopyFrom(Helper.GetMQOptions());
                    });
                    services.AddNetRpcGrpcService(i => i.AddPort("0.0.0.0", 50001));

                    services.AddNetRpcMiddleware(i =>
                    {
                        i.UseMiddleware<CallbackThrottlingMiddleware>(500);
                        i.UseMiddleware<StreamCallBackMiddleware>(10);
                        i.UseMiddleware<ExMiddleware>();
                    });
                    services.AddNetRpcContractSingleton<IService, Service>();
                }).ConfigureLogging((context, builder) =>
                {
                    builder.AddConsole();
                })
                .Build();
            await h.StartAsync();
        }
    }

    internal class Service : IService
    {
        private readonly ILogger<Service> _log;

        public Service(ILogger<Service> log)
        {
            _log = log;
        }
    
        public Task Call3(string s)
        {
            _log.LogInformation($"call, {s}");
            return Task.CompletedTask;
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