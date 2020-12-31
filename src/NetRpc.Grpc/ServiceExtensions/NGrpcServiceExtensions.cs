using System;
using Grpc.AspNetCore.Server;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NetRpc;
using NetRpc.Grpc;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class NGrpcServiceExtensions
    {
        public static IServiceCollection AddNGrpcService(this IServiceCollection services, Action<GrpcServiceOptions>? configureOptions = null)
        {
            if (configureOptions != null)
                services.AddGrpc(configureOptions);
            else
                services.AddGrpc();

            services.AddNService();
            return services;
        }

        public static IApplicationBuilder UseNGrpc(this IApplicationBuilder app)
        {
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGrpcService<MessageCallImpl>();
                endpoints.MapGet("/",
                    async context =>
                    {
                        await context.Response.WriteAsync(
                            "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
                    });
            });
            return app;
        }

        public static IServiceCollection AddNGrpcGateway<TService>(this IServiceCollection services,
            Action<GrpcClientOptions>? grpcClientConfigureOptions = null,
            Action<NClientOptions>? clientConfigureOptions = null,
            ServiceLifetime serviceLifetime = ServiceLifetime.Singleton) where TService : class
        {
            services.AddNGrpcClient(grpcClientConfigureOptions, clientConfigureOptions, serviceLifetime);
            services.Configure<NClientOptions>(i => i.ForwardHeader = true);
            services.AddNGrpcClientContract<TService>(serviceLifetime);
            services.AddNServiceContract(typeof(TService),
                p => ((IClientProxy<TService>) p.GetService(typeof(IClientProxy<TService>))).Proxy, serviceLifetime);
            return services;
        }

        public static IServiceCollection AddNGrpcClient(this IServiceCollection services,
            Action<GrpcClientOptions>? grpcClientConfigureOptions = null,
            Action<NClientOptions>? clientConfigureOptions = null,
            ServiceLifetime serviceLifetime = ServiceLifetime.Singleton)
        {
            if (grpcClientConfigureOptions != null)
                services.Configure(grpcClientConfigureOptions);

            services.AddNClientByClientConnectionFactory<GrpcClientConnectionFactory>(clientConfigureOptions, serviceLifetime);

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

            services.AddSingleton<IOrphanClientProxyProvider, OrphanNGrpcClientProxyProvider>();

            return services;
        }

        public static IServiceCollection AddNGrpcClientContract<TService>(this IServiceCollection services,
            ServiceLifetime serviceLifetime = ServiceLifetime.Singleton) where TService : class
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