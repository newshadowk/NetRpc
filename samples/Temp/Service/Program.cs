using System;
using System.Diagnostics;
using System.Threading.Tasks;
using DataContract;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
                    //builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, MixLoggerProvider>());
                    //builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, MixLoggerProvider>());
                    //builder.Services.AddSingleton<ILoggerProvider, MixLoggerProvider>();
                    //builder.Services.AddSingleton<ILoggerProvider, MixLoggerProvider2>();
                    builder.AddConsole();
                })
                .Build();
            await h.StartAsync();
        }
    }

    public class MixLoggerProvider : ILoggerProvider
    {
        public void Dispose()
        {

        }

        public ILogger CreateLogger(string categoryName)
        {
            return new MyLog(categoryName, "1");
        }
    }

    public class MixLoggerProvider2 : ILoggerProvider
    {
        public void Dispose()
        {

        }

        public ILogger CreateLogger(string categoryName)
        {
            return new MyLog(categoryName, "2");
        }
    }

    public class MyLog : ILogger
    {
        private readonly string _categoryName;
        private readonly string _tag;

        public MyLog(string categoryName, string tag)
        {
            _categoryName = categoryName;
            _tag = tag;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            Console.WriteLine($"{_tag}, {_categoryName}, {logLevel}, {eventId}, {state}");
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
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
            EventId id =new EventId();

            var beginScope = _log.BeginScope("f1", new object[] {"1", "2"});
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