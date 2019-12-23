using System;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NetRpc;
using NetRpc.Grpc;

#if NETCOREAPP3_1
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
#endif

namespace Microsoft.Extensions.DependencyInjection
{
    public static class GrpcServiceExtensions
    {
        public static IServiceCollection AddNetRpcGrpcService(this IServiceCollection services, Action<GrpcServiceOptions> configureOptions = null)
        {
            if (configureOptions != null)
                services.Configure(configureOptions);

            services.AddNetRpcService();
#if NETCOREAPP3_1
            services.AddGrpc();
#else
            services.AddHostedService<GrpcServiceProxy>();
#endif
            return services;
        }

#if NETCOREAPP3_1
        public static IApplicationBuilder UseNetRpcGrpc(this IApplicationBuilder app)
        {
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGrpcService<MessageCallImpl>();
                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
                });
            });
            return app;
        }
#endif

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