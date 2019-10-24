using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DataContract;
using DataContract1;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NetRpc;
using NetRpc.Grpc;
using ActionExecutedContext = NetRpc.ActionExecutedContext;
using ActionExecutionDelegate = NetRpc.ActionExecutionDelegate;
using ActionFilterAttribute = NetRpc.ActionFilterAttribute;
using IAsyncActionFilter = Microsoft.AspNetCore.Mvc.Filters.IAsyncActionFilter;
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
            var o = new GrpcServiceOptions();
            o.AddPort("0.0.0.0", 50001);
            var host = new HostBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddNetRpcGrpcService(i => { i.AddPort("0.0.0.0", 50001); });
                    services.AddNetRpcContractSingleton<IService, Service>();
                })
                .Build();


            await host.RunAsync();
        }
    }

    [F("out")]
    internal class Service : IService
    {
        [RouteTo(typeof(IService1), "Call")]
        public async Task<Ret> Call(InParam p, Stream stream, Action<int> progs, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        [F("in1")]
        [F("in2")]
        public async Task<string> Call2(string s)
        {
            //throw new Exception("123");
            Console.WriteLine("call2");
            return "ret";
        }
    }

    public class F : ActionFilterAttribute
    {
        public string Name { get; set; }

        public F(string name)
        {
            Name = name;
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            context.ExceptionHandled = true;
            Console.WriteLine($"{Name} OnActionExecuted, {context.Canceled}, {context.Exception?.Message}");
        }

        public override void OnActionExecuting(ServiceContext context)
        {
            Console.WriteLine($"{Name} OnActionExecuting 1");
        }

        public override async Task OnActionExecutionAsync(ServiceContext context, ActionExecutionDelegate next)
        {
            Console.WriteLine($"{Name} OnActionExecutionAsync 1");
            await base.OnActionExecutionAsync(context, next);
            Console.WriteLine($"{Name} OnActionExecutionAsync 2");
        }
    }
}