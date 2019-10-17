using System;
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

        public static IServiceCollection AddNetRpcRabbitMQClient(this IServiceCollection services,
            Action<RabbitMQClientOptions> mQClientConfigureOptions = null,
            Action<NetRpcClientOption> clientConfigureOptions = null)
        {
            if (mQClientConfigureOptions != null)
                services.Configure(mQClientConfigureOptions);
            services.AddNetRpcClientByClientConnectionFactory<RabbitMQClientConnectionFactory>(clientConfigureOptions);
            services.AddSingleton<IClientProxyProvider, RabbitMqClientProxyProvider>();
            return services;
        }

        public static IServiceCollection AddNetRpcRabbitMQGateway<TService>(this IServiceCollection services,
            Action<RabbitMQClientOptions> mQClientConfigureOptions = null,
            Action<NetRpcClientOption> clientConfigureOptions = null)
        {
            services.AddNetRpcRabbitMQClient(mQClientConfigureOptions, clientConfigureOptions);
            services.AddNetRpcClientContract<TService>();
            services.AddNetRpcContractSingleton(typeof(TService),
                p => ((ClientProxy<TService>)p.GetService(typeof(ClientProxy<TService>))).Proxy);
            return services;
        }
    }
}