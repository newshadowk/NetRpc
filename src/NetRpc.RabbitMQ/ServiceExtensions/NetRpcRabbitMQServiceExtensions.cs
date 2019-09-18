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
            services.AddHostedService<RabbitMQServiceProxy>();
            return services;
        }

        public static IServiceCollection AddNetRpcRabbitMQClient<TService>(this IServiceCollection services,
            Action<RabbitMQClientOptions> mQClientConfigureOptions = null,
            Action<NetRpcClientOption> clientConfigureOptions = null)
        {
            if (mQClientConfigureOptions != null)
                services.Configure(mQClientConfigureOptions);
            services.AddNetRpcClient<RabbitMQClientConnectionFactory, TService>(clientConfigureOptions);
            return services;
        }

        public static IServiceCollection AddNetRpcRabbitMQGateway<TService>(this IServiceCollection services,
            Action<RabbitMQClientOptions> mQClientConfigureOptions = null,
            Action<NetRpcClientOption> clientConfigureOptions = null)
        {
            services.AddNetRpcRabbitMQClient<TService>(mQClientConfigureOptions, clientConfigureOptions);
            services.AddNetRpcContractSingleton(typeof(TService),
                p => ((ClientProxy<TService>) p.GetService(typeof(ClientProxy<TService>))).Proxy);
            return services;
        }
    }
}