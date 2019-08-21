using System;
using Microsoft.Extensions.DependencyInjection;

namespace NetRpc.Grpc
{
    public static class GrpcServiceExtensions
    {
        public static IServiceCollection AddNetRpcGrpcService(this IServiceCollection services, 
            Action<GrpcServiceOptions> grpcServiceConfigureOptions, 
            Action<MiddlewareOptions> middlewareConfigureOptions = null)
        {
            services.Configure(grpcServiceConfigureOptions);
            services.AddNetRpcService(middlewareConfigureOptions);
            services.AddHostedService<GrpcServiceProxy>();
            return services;
        }

        public static IServiceCollection AddNetRpcGrpcClient<TService>(this IServiceCollection services, 
            Action<GrpcClientOptions> grpcClientChannelConfigureOptions = null, 
            Action<NetRpcClientOption> clientConfigureOptions = null)
        {
            if (grpcClientChannelConfigureOptions != null)
                services.Configure(grpcClientChannelConfigureOptions);
            services.AddSingleton<GrpcClientProxy<TService>>();
            services.AddNetRpcClient<GrpcClientConnectionFactory, TService>(clientConfigureOptions);
            return services;
        }
    }
}