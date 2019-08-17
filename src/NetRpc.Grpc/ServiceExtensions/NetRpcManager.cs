using System;
using System.Collections.Generic;
using Grpc.Core;
using Microsoft.Extensions.Hosting;

namespace NetRpc.Grpc
{
    public static class NetRpcManager
    {
        public static IHost CreateHost(GrpcServiceOptions grpcServiceOptions, MiddlewareOptions middlewareOptions, params Type[] instanceTypes)
        {
            return new HostBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddNetRpcGrpcService(i =>
                        {
                            if (grpcServiceOptions != null)
                                i.Ports = grpcServiceOptions.Ports;
                        },
                        i =>
                        {
                            if (middlewareOptions != null)
                                i.Items = middlewareOptions.Items;
                        });
                    services.AddNetRpcServiceContract(instanceTypes);
                })
                .Build();
        }

        public static ClientProxy<TService> CreateClientProxy<TService>(Channel channel, bool isWrapFaultException = true, int timeoutInterval = 1200000,
            int hearbeatInterval = 10000)
        {
            var opt = new GrpcClientConnectionFactoryOptions(new GrpcClientOptions {Channel = channel});
            return CreateClientProxy<TService>(opt, isWrapFaultException, timeoutInterval, hearbeatInterval);
        }

        public static ClientProxy<TService> CreateClientProxy<TService>(GrpcClientConnectionFactoryOptions options, bool isWrapFaultException = true,
            int timeoutInterval = 1200000, int hearbeatInterval = 10000)
        {
            return new GrpcClientProxy<TService>(options.Factory,
                new SimpleOptionsMonitor<NetRpcClientOption>(new NetRpcClientOption
                    {
                        IsWrapFaultException = isWrapFaultException,
                        TimeoutInterval = timeoutInterval,
                        HearbeatInterval = hearbeatInterval
                    }
                ));
        }

        public static ClientProxy<TService> CreateClientProxy<TService>(string host, int port, string publicKey, string sslTargetName,
            bool isWrapFaultException = true, int timeoutInterval = 1200000, int hearbeatInterval = 10000)
        {
            var ssl = new SslCredentials(publicKey);
            var options = new List<ChannelOption>();
            options.Add(new ChannelOption(ChannelOptions.SslTargetNameOverride, sslTargetName));
            var channel = new Channel(host, port, ssl, options);
            return CreateClientProxy<TService>(channel, isWrapFaultException, timeoutInterval, hearbeatInterval);
        }
    }
}