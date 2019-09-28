using System;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NetRpc;
using NetRpc.Grpc;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class GrpcServiceExtensions
    {
        public static IServiceCollection AddNetRpcGrpcService(this IServiceCollection services, Action<GrpcServiceOptions> configureOptions)
        {
            services.Configure(configureOptions);
            services.AddNetRpcService();
            services.AddHostedService<GrpcServiceProxy>();
            return services;
        }

        public static IServiceCollection AddNetRpcGrpcGateway<TService>(this IServiceCollection services,
            Action<GrpcClientOptions> grpcClientConfigureOptions = null,
            Action<NetRpcClientOption> clientConfigureOptions = null)
        {
            services.AddNetRpcGrpcClient<TService>(grpcClientConfigureOptions, clientConfigureOptions);
            services.AddNetRpcContractSingleton(typeof(TService),
                p => ((ClientProxy<TService>) p.GetService(typeof(ClientProxy<TService>))).Proxy);
            return services;
        }

        public static IServiceCollection AddNetRpcGrpcClient<TService>(this IServiceCollection services,
            Action<GrpcClientOptions> grpcClientConfigureOptions = null,
            Action<NetRpcClientOption> clientConfigureOptions = null)
        {
            if (grpcClientConfigureOptions != null)
                services.Configure(grpcClientConfigureOptions);
            services.TryAddSingleton<GrpcClientProxy<TService>>();
            services.AddNetRpcClient<GrpcClientConnectionFactory, TService>(clientConfigureOptions);
            services.AddSingleton<IClientProxyProvider, GrpcClientProxyProvider>();
            return services;
        }
    }
}