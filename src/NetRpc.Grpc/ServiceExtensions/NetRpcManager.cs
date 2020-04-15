using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace NetRpc.Grpc
{
    public static class NetRpcManager
    {
#if !NETCOREAPP3_1
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
                        services.AddNetRpcServiceContract(contract.ContractInfo.Type, contract.InstanceType);
                })
                .Build();
        }
#endif

        public static ClientProxy<TService> CreateClientProxy<TService>(GrpcClientOptions options, int timeoutInterval = 1200000,
            int hearbeatInterval = 10000)
        {
            var opt = new GrpcClientConnectionFactoryOptions(options);
            return CreateClientProxy<TService>(opt, timeoutInterval, hearbeatInterval);
        }

        public static ClientProxy<TService> CreateClientProxy<TService>(GrpcClientConnectionFactoryOptions options, int timeoutInterval = 1200000,
            int hearbeatInterval = 10000)
        {
            return new GrpcClientProxy<TService>(options.Factory,
                new SimpleOptions<NetRpcClientOption>(new NetRpcClientOption
                    {
                        TimeoutInterval = timeoutInterval,
                        HearbeatInterval = hearbeatInterval
                    }
                ), null, NullLoggerFactory.Instance);
        }
    }
}