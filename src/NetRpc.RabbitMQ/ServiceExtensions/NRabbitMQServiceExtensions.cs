﻿using System;
using NetRpc;
using NetRpc.RabbitMQ;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class NRabbitMQServiceExtensions
    {
        public static IServiceCollection AddNRabbitMQService(this IServiceCollection services,
            Action<RabbitMQServiceOptions>? configureOptions = null)
        {
            if (configureOptions != null)
                services.Configure(configureOptions);
            services.AddNService();
            services.AddHostedService<RabbitMQHostedService>();
            return services;
        }

        public static IServiceCollection AddNRabbitMQClient(this IServiceCollection services,
            Action<RabbitMQClientOptions>? mQClientConfigureOptions = null,
            Action<NClientOptions>? clientConfigureOptions = null,
            ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
        {
            if (mQClientConfigureOptions != null)
                services.Configure(mQClientConfigureOptions);
            services.AddLogging();
            services.AddNClientByClientConnectionFactory<RabbitMQClientConnectionFactory>(clientConfigureOptions, serviceLifetime);
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

        public static IServiceCollection AddNRabbitMQGateway<TService>(this IServiceCollection services,
            Action<RabbitMQClientOptions>? mQClientConfigureOptions = null,
            Action<NClientOptions>? clientConfigureOptions = null,
            ServiceLifetime serviceLifetime = ServiceLifetime.Scoped) where TService : class
        {
            services.AddNRabbitMQClient(mQClientConfigureOptions, clientConfigureOptions, serviceLifetime);
            services.Configure<NClientOptions>(i => i.ForwardHeader = true);
            services.AddNClientContract<TService>(serviceLifetime);
            services.AddNServiceContract(typeof(TService),
                p => ((ClientProxy<TService>) p.GetService(typeof(ClientProxy<TService>))!).Proxy,
                serviceLifetime);
            return services;
        }
    }
}