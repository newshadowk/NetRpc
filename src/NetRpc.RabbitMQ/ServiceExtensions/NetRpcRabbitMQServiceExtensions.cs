using System;
using Microsoft.Extensions.DependencyInjection;

namespace NetRpc.RabbitMQ
{
    public static class NetRpcRabbitMQServiceExtensions
    {
        public static IServiceCollection AddNetRpcRabbitMQService(this IServiceCollection services, 
            Action<RabbitMQServiceOptions> rabbitMQServiceConfigureOptions = null, 
            Action<MiddlewareOptions> middlewareConfigureOptions = null)
        {
            if (rabbitMQServiceConfigureOptions != null)
                services.Configure(rabbitMQServiceConfigureOptions);
            services.AddHostedService<RabbitMQServiceProxy>();
            services.AddNetRpcService(middlewareConfigureOptions);
            return services;
        }

        public static IServiceCollection AddNetRpcRabbitMQClient<TService>(this IServiceCollection services, 
            Action<RabbitMQClientOptions> clientMQConfigureOptions = null, 
            Action<NetRpcClientOption> clientConfigureOptions = null)
        {
            if (clientMQConfigureOptions != null)
                services.Configure(clientMQConfigureOptions);
            services.AddNetRpcClient<RabbitMQClientConnectionFactory, TService>(clientConfigureOptions);
            return services;
        }
    }

    public class RabbitMQServiceOptions
    {
        public MQOptions Value { get; set; }
    }

    public class RabbitMQClientOptions
    {
        public MQOptions Value { get; set; }
    }
}