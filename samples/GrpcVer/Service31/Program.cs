using System;
using System.Threading;
using System.Threading.Tasks;
using DataContract;
using Grpc.Core;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NetRpc;
using NetRpc.Grpc;
using Proxy.Grpc;

namespace Service
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var host = WebHost.CreateDefaultBuilder(null)
                .ConfigureKestrel(i =>
                {
                    i.Limits.MaxRequestBodySize = 10737418240; //10G
                    i.ListenAnyIP(5000);
                    i.ListenAnyIP(5001, listenOptions =>
                    {
                        listenOptions.UseHttps(
                            @"1.pfx", "aaaa1111");
                    });
                })
                .ConfigureServices(services =>
                {
                    services.AddNetRpcGrpcService();
                    services.AddNetRpcContractSingleton<IService, Service>();
                })
                .Configure(app =>
                {
                    app.UseNetRpcGrpc();
                }).Build();

            await host.RunAsync();

            //var host = WebHost.CreateDefaultBuilder(null)
            //    .ConfigureKestrel(i =>
            //    {
            //        i.Limits.MaxRequestBodySize = 10737418240; //10G
            //    })
            //    .ConfigureServices(services =>
            //    {
            //        services.AddGrpc();

            //    })
            //    .Configure(app =>
            //    {
            //        app.UseRouting();
            //        app.UseEndpoints(endpoints =>
            //        {
            //            endpoints.MapGrpcService<MessageCallImpl2>();
            //            endpoints.MapGet("/", async context =>
            //            {
            //                await context.Response.WriteAsync("Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
            //            });
            //        });
            //    }).Build();

            //await host.RunAsync();
        }
    }

    internal class Service : IService
    {
        public async Task Call(string s)
        {
            Console.WriteLine($"Receive: {s}");
        }
    }

    public sealed class MessageCallImpl2 : MessageCall.MessageCallBase
    {

        public MessageCallImpl2()
        {
        }


        public override async Task DuplexStreamingServerMethod(IAsyncStreamReader<StreamBuffer> requestStream, IServerStreamWriter<StreamBuffer> responseStream,
            ServerCallContext context)
        {
            Task.Run(async() =>
            {
                try
                {
                    while (await requestStream.MoveNext(CancellationToken.None))
                        Console.WriteLine("count:" + requestStream.Current.Body.Length);
                    Console.WriteLine("end ---------");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            });
        }
    }
}