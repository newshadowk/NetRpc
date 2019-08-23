using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace NetRpc.Grpc
{
    public static class GrpcServiceExtensions
    {
        public static IServiceCollection AddNetRpcGrpcService(this IServiceCollection services, Action<GrpcServiceOptions> grpcServiceConfigureOptions)
        {
            services.Configure(grpcServiceConfigureOptions);
            services.AddNetRpcService();
            services.AddHostedService<GrpcServiceProxy>();
            return services;
        }

        public static IServiceCollection AddNetRpcGrpcClient<TService>(this IServiceCollection services, 
            Action<GrpcClientOptions> grpcClientChannelConfigureOptions = null, 
            Action<NetRpcClientOption> clientConfigureOptions = null)
        {
            if (grpcClientChannelConfigureOptions != null)
                services.Configure(grpcClientChannelConfigureOptions);
            services.TryAddSingleton<GrpcClientProxy<TService>>();
            services.AddNetRpcClient<GrpcClientConnectionFactory, TService>(clientConfigureOptions);
            return services;
        }
    }
}