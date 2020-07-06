using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace NetRpc.Grpc
{
    public static class NManager
    {
#if !NETCOREAPP3_1
        public static IHost CreateHost(NGrpcServiceOptions nGrpcServiceOptions, MiddlewareOptions middlewareOptions, params Contract[] contracts)
        {
            return new HostBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddNGrpcService(i =>
                    {
                        if (nGrpcServiceOptions != null)
                            i.Ports = nGrpcServiceOptions.Ports;
                    });
                    services.AddNRpcMiddleware(i =>
                    {
                        if (middlewareOptions != null)
                            i.AddItems(middlewareOptions.GetItems());
                    });

                    foreach (var contract in contracts)
                        services.AddNRpcServiceContract(contract.ContractInfo.Type, contract.InstanceType);
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
                new SimpleOptions<NClientOption>(new NClientOption
                    {
                        TimeoutInterval = timeoutInterval,
                        HearbeatInterval = hearbeatInterval
                    }
                ), new NullOptions<ClientMiddlewareOptions>(), 
                ActionExecutingContextAccessor.Default, 
                null,
                NullLoggerFactory.Instance);
        }
    }
}