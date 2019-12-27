using System.Collections.Generic;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

#if NETCOREAPP3_1
using Channel = Grpc.Net.Client.GrpcChannel;
#else
using Channel = Grpc.Core.Channel;
#endif

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
                        services.AddNetRpcContractSingleton(contract.ContractInfo.Type, contract.InstanceType);
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
                ), null, NullLoggerFactory.Instance);
        }

#if !NETCOREAPP3_1
        public static ClientProxy<TService> CreateClientProxy<TService>(string host, int port, string publicKey, string sslTargetName = null, int timeoutInterval = 1200000, int hearbeatInterval = 10000)
        {
            var ssl = new SslCredentials(publicKey);
            Channel channel;
            if (sslTargetName == null)
            {
                channel = new Channel(host, port, ssl);
            }
            else
            {
                var options = new List<ChannelOption>();
                options.Add(new ChannelOption(ChannelOptions.SslTargetNameOverride, sslTargetName));
                channel = new Channel(host, port, ssl, options);
            }

            return CreateClientProxy<TService>(channel, timeoutInterval, hearbeatInterval);
        }
#endif
    }
}