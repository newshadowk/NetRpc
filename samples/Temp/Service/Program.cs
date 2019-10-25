using System;
using System.Threading.Tasks;
using DataContract;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
                    services.AddNetRpcRabbitMQService(i => { i.CopyFrom(Helper.GetMQOptions()); });
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
        public Task CallAsync(string s)
        {
            Console.WriteLine($"CallAsync {s}");
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

        public async Task InvokeAsync(ActionExecutingContext context)
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