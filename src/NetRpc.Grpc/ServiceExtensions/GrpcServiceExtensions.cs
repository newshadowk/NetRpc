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
            services.AddNetRpcGrpcClient(grpcClientConfigureOptions, clientConfigureOptions);
            services.AddNetRpcGrpcClientContract<TService>();
            services.AddNetRpcContractSingleton(typeof(TService),
                p => ((IClientProxy<TService>) p.GetService(typeof(IClientProxy<TService>))).Proxy);
            return services;
        }

        public static IServiceCollection AddNetRpcGrpcClient(this IServiceCollection services,
            Action<GrpcClientOptions> grpcClientConfigureOptions = null,
            Action<NetRpcClientOption> clientConfigureOptions = null)
        {
            if (grpcClientConfigureOptions != null)
                services.Configure(grpcClientConfigureOptions);

            services.AddNetRpcClientByClientConnectionFactory<GrpcClientConnectionFactory>(clientConfigureOptions);
            services.AddSingleton<IClientProxyProvider, GrpcClientProxyProvider>();
            return services;
        }

        public static IServiceCollection AddNetRpcGrpcClientContract<TService>(this IServiceCollection services)
        {
            services.TryAddSingleton<IClientProxy<TService>, GrpcClientProxy<TService>>();
            services.TryAddSingleton(typeof(TService), p => p.GetService<IClientProxy<TService>>().Proxy);
            return services;
        }
    }
}