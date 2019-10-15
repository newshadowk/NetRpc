using System.Collections.Generic;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace NetRpc.Grpc
{
    public static class NetRpcManager
    {
        public static IHost CreateHost(GrpcServiceOptions grpcServiceOptions, MiddlewareOptions middlewareOptions, params Contract[] contracts)
        {
            return new HostBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddNetRpcGrpcService(i =>
                    {
                        if (grpcServiceOptions != null)
                            i.Ports = grpcServiceOptions.Ports;
                    });
                    services.AddNetRpcMiddleware(i =>
                    {
                        if (middlewareOptions != null)
                            i.AddItems(middlewareOptions.GetItems());
                    });

                    foreach (var contract in contracts)
                        services.AddNetRpcContractSingleton(contract.ContractType, contract.InstanceType);
                })
                .Build();
        }

        public static ClientProxy<TService> CreateClientProxy<TService>(Channel channel, int timeoutInterval = 1200000,
            int hearbeatInterval = 10000)
        {
            var opt = new GrpcClientConnectionFactoryOptions(new GrpcClientOptions {Channel = channel});
            return CreateClientProxy<TService>(opt, timeoutInterval, hearbeatInterval);
        }

        public static ClientProxy<TService> CreateClientProxy<TService>(GrpcClientConnectionFactoryOptions options, int timeoutInterval = 1200000,
            int hearbeatInterval = 10000)
        {
            return new GrpcClientProxy<TService>(options.Factory,
                new SimpleOptionsMonitor<NetRpcClientOption>(new NetRpcClientOption
                    {
                        TimeoutInterval = timeoutInterval,
                        HearbeatInterval = hearbeatInterval
                    }
                ), null);
        }

        public static ClientProxy<TService> CreateClientProxy<TService>(string host, int port, string publicKey, string sslTargetName,
            int timeoutInterval = 1200000, int hearbeatInterval = 10000)
        {
            var ssl = new SslCredentials(publicKey);
            var options = new List<ChannelOption>();
            options.Add(new ChannelOption(ChannelOptions.SslTargetNameOverride, sslTargetName));
            var channel = new Channel(host, port, ssl, options);
            return CreateClientProxy<TService>(channel, timeoutInterval, hearbeatInterval);
        }
    }
}