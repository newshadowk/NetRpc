using System;
using System.Text;
using System.Threading.Tasks;
using DataContract;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetRpc.RabbitMQ;
using Proxy.RabbitMQ;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Helper = TestHelper.Helper;

namespace Service;

internal class Program
{
    private static async Task Main(string[] args)
    {
        await T1();
    }

    private static async Task T1()
    {
        var grpcHost = Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.ConfigureKestrel((_, options) =>
                    {
                        options.ListenAnyIP(50001, listenOptions => listenOptions.Protocols = HttpProtocols.Http2);
                    })
                    .ConfigureServices((_, services) =>
                    {
                        services.AddNGrpcService();
                        services.AddNServiceContract<IServiceAsync, ServiceAsync>();

                        services.AddNRabbitMQService(i => i.CopyFrom(Helper.GetMQOptions()));
                        services.Configure<QueueStatusOptions>(i => i.CopyFrom(Helper.GetMQOptions()));
                        services.AddNRabbitMQQueueStatus();

                    }).ConfigureLogging((_, loggingBuilder) =>
                    {
                        loggingBuilder.AddConsole();
                    }).Configure(app => { app.UseNGrpc(); });
            }).Build();
        await grpcHost.RunAsync();
    }
    
    private static async Task T0()
    {
        Console.WriteLine("start");

        var c = Helper.GetMQOptions().CreateMainConnectionFactory().CreateConnection();
        var ch = c.CreateModel();
        ch.QueueDeclare("rpc_test2", false, false, true, null);
        ch.BasicQos(0, 1, false);
        var consumer = new AsyncEventingBasicConsumer(ch);
        consumer.Received += async (_, e) =>
        {
            Console.WriteLine($"{Encoding.UTF8.GetString(e.Body.Span)} start.");
            await Task.Delay(1000);
            Console.WriteLine($"{Encoding.UTF8.GetString(e.Body.Span)} end.");
            ch.BasicAck(e.DeliveryTag, false);
        };
        ch.BasicConsume("rpc_test2", false, consumer);
    }
}