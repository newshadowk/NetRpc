using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using DataContract;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Service
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(builder =>
                {
                    builder.ConfigureKestrel((context, options) =>
                        {
                            options.ListenAnyIP(50001, listenOptions => listenOptions.Protocols = HttpProtocols.Http2);
                        })
                        .ConfigureServices((context, services) =>
                        {
                            services.AddNGrpcService();
                            services.AddNServiceContract<IServiceAsync, ServiceAsync>();
                        })
                        .Configure(app =>
                        {
                            app.UseNGrpc(); 
                        })
                        .ConfigureLogging(loggingBuilder =>
                        {
                            loggingBuilder.AddDebug();
                        });
                })
                .Build();
            await host.RunAsync();
        }
    }

    public class ServiceAsync : IServiceAsync
    {
        private readonly ILogger<ServiceAsync> _logger;

        public ServiceAsync(ILogger<ServiceAsync> logger)
        {
            _logger = logger;
        }

        public async Task CallAsync(string s)
        {
            _logger.LogInformation($"Receive: {s}");
            Console.WriteLine($"Receive: {s}");
            throw new Exception();
        }
    }
}