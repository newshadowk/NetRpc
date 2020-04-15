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
    public static class NetRpcGrpcServiceExtensions
    {
#if !NETCOREAPP3_1
        public static IServiceCollection AddNetRpcGrpcService(this IServiceCollection services, Action<GrpcServiceOptions> configureOptions = null)
        {
            if (configureOptions != null)
                services.Configure(configureOptions);

            services.AddNetRpcService();
            services.AddHostedService<GrpcServiceProxy>();
            services.AddSingleton(typeof(MessageCallImpl));
            return services;
        }
#else
        public static IServiceCollection AddNetRpcGrpcService(this IServiceCollection services, Action<Grpc.AspNetCore.Server.GrpcServiceOptions> configureOptions = null)
        {
            if (configureOptions != null)
                services.AddGrpc(configureOptions);
            else
                services.AddGrpc();

            services.AddNetRpcService();
            return services;
        }
#endif

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
            Action<NetRpcClientOption> clientConfigureOptions = null,
            ServiceLifetime serviceLifetime = ServiceLifetime.Singleton)
        {
            services.AddNetRpcGrpcClient(grpcClientConfigureOptions, clientConfigureOptions, serviceLifetime);
            services.AddNetRpcGrpcClientContract<TService>(serviceLifetime);
            services.AddNetRpcServiceContract(typeof(TService),
                p => ((IClientProxy<TService>) p.GetService(typeof(IClientProxy<TService>))).Proxy, 
                serviceLifetime);
            return services;
        }

        public static IServiceCollection AddNetRpcGrpcClient(this IServiceCollection services,
            Action<GrpcClientOptions> grpcClientConfigureOptions = null,
            Action<NetRpcClientOption> clientConfigureOptions = null,
            ServiceLifetime serviceLifetime = ServiceLifetime.Singleton)
        {
            if (grpcClientConfigureOptions != null)
                services.Configure(grpcClientConfigureOptions);

            services.AddNetRpcClientByClientConnectionFactory<GrpcClientConnectionFactory>(clientConfigureOptions, serviceLifetime);

            switch (serviceLifetime)
            {
                case ServiceLifetime.Singleton:
                    services.AddSingleton<IClientProxyProvider, GrpcClientProxyProvider>();
                    break;
                case ServiceLifetime.Scoped:
                    services.AddScoped<IClientProxyProvider, GrpcClientProxyProvider>();
                    break;
                case ServiceLifetime.Transient:
                    services.AddTransient<IClientProxyProvider, GrpcClientProxyProvider>();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(serviceLifetime), serviceLifetime, null);
            }

            return services;
        }

        public static IServiceCollection AddNetRpcGrpcClientContract<TService>(this IServiceCollection services,
            ServiceLifetime serviceLifetime = ServiceLifetime.Singleton)
        {
            switch (serviceLifetime)
            {
                case ServiceLifetime.Singleton:
                    services.TryAddSingleton<IClientProxy<TService>, GrpcClientProxy<TService>>();
                    services.TryAddSingleton(typeof(TService), p => p.GetService<IClientProxy<TService>>().Proxy);
                    break;
                case ServiceLifetime.Scoped:
                    services.TryAddScoped<IClientProxy<TService>, GrpcClientProxy<TService>>();
                    services.TryAddScoped(typeof(TService), p => p.GetService<IClientProxy<TService>>().Proxy);
                    break;
                case ServiceLifetime.Transient:
                    services.TryAddTransient<IClientProxy<TService>, GrpcClientProxy<TService>>();
                    services.TryAddTransient(typeof(TService), p => p.GetService<IClientProxy<TService>>().Proxy);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(serviceLifetime), serviceLifetime, null);
            }

            return services;
        }
    }
}