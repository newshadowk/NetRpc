using System;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NetRpc;
using NetRpc.RabbitMQ;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class NetRpcRabbitMQServiceExtensions
    {
        public static IServiceCollection AddNetRpcRabbitMQService(this IServiceCollection services,
            Action<RabbitMqServiceOptions> configureOptions = null)
        {
            if (configureOptions != null)
                services.Configure(configureOptions);
            services.AddNetRpcService();
            services.AddHostedService<RabbitMQServiceProxy>();
            return services;
        }

        public static IServiceCollection AddNetRpcRabbitMQClient<TService>(this IServiceCollection services,
            Action<RabbitMQClientOptions> mQClientConfigureOptions = null,
            Action<NetRpcClientOption> clientConfigureOptions = null)
        {
            if (mQClientConfigureOptions != null)
                services.Configure(mQClientConfigureOptions);

            if (clientConfigureOptions != null)
                services.Configure(clientConfigureOptions);

            services.AddNetRpcClient<RabbitMQClientConnectionFactory, TService>();
            services.AddSingleton<IClientProxyProvider, RabbitMqClientProxyProvider>();
            return services;
        }

        public static IServiceCollection AddNetRpcRabbitMQGateway<TService>(this IServiceCollection services,
            Action<RabbitMQClientOptions> mQClientConfigureOptions = null,
            Action<NetRpcClientOption> clientConfigureOptions = null)
        {
            services.AddNetRpcRabbitMQClient<TService>(mQClientConfigureOptions, clientConfigureOptions);
            services.AddNetRpcContractSingleton(typeof(TService),
                p => ((ClientProxy<TService>)p.GetService(typeof(ClientProxy<TService>))).Proxy);
            return services;
        }
    }
}