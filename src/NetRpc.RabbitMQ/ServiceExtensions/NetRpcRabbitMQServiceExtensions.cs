using System;
using NetRpc;
using NetRpc.RabbitMQ;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class NetRpcRabbitMQServiceExtensions
    {
        public static IServiceCollection AddNetRpcRabbitMQService(this IServiceCollection services,
            Action<RabbitMQServiceOptions> configureOptions = null)
        {
            if (configureOptions != null)
                services.Configure(configureOptions);
            services.AddNetRpcService();
            services.AddHostedService<RabbitMQServiceProxy>();
            return services;
        }

        public static IServiceCollection AddNetRpcRabbitMQClient(this IServiceCollection services,
            Action<RabbitMQClientOptions> mQClientConfigureOptions = null,
            Action<NetRpcClientOption> clientConfigureOptions = null,
            ServiceLifetime serviceLifetime = ServiceLifetime.Singleton)
        {
            if (mQClientConfigureOptions != null)
                services.Configure(mQClientConfigureOptions);
            services.AddNetRpcClientByClientConnectionFactory<RabbitMQClientConnectionFactory>(clientConfigureOptions, serviceLifetime);
            switch (serviceLifetime)
            {
                case ServiceLifetime.Singleton:
                    services.AddSingleton<IClientProxyProvider, RabbitMQClientProxyProvider>();
                    break;
                case ServiceLifetime.Scoped:
                    services.AddScoped<IClientProxyProvider, RabbitMQClientProxyProvider>();
                    break;
                case ServiceLifetime.Transient:
                    services.AddTransient<IClientProxyProvider, RabbitMQClientProxyProvider>();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(serviceLifetime), serviceLifetime, null);
            }

            services.AddSingleton<IOrphanClientProxyProvider, OrphanRabbitMQClientProxyProvider>();

            return services;
        }

        public static IServiceCollection AddNetRpcRabbitMQGateway<TService>(this IServiceCollection services,
            Action<RabbitMQClientOptions> mQClientConfigureOptions = null,
            Action<NetRpcClientOption> clientConfigureOptions = null,
            ServiceLifetime serviceLifetime = ServiceLifetime.Singleton)
        {
            services.AddNetRpcRabbitMQClient(mQClientConfigureOptions, clientConfigureOptions, serviceLifetime);
            services.AddNetRpcClientContract<TService>(serviceLifetime);
            services.AddNetRpcServiceContract(typeof(TService),
                p => ((ClientProxy<TService>)p.GetService(typeof(ClientProxy<TService>))).Proxy,
                serviceLifetime);
            return services;
        }
    }
}