using Microsoft.Extensions.DependencyInjection.Extensions;
using NetRpc;
using NetRpc.RabbitMQ;
using Proxy.RabbitMQ;

namespace Microsoft.Extensions.DependencyInjection;

public static class NRabbitMQServiceExtensions
{
    public static IServiceCollection AddNRabbitMQService(this IServiceCollection services,
        Action<MQServiceOptions>? configureOptions = null)
    {
        if (configureOptions != null)
            services.Configure(configureOptions);
        services.AddNService();
        services.AddHostedService<RabbitMQHostedService>();
        return services;
    }

    public static IServiceCollection AddNRabbitMQClient(this IServiceCollection services,
        Action<MQClientOptions>? configureMQClientOptions = null,
        Action<NClientOptions>? configureClientOptions = null)
    {
        if (configureMQClientOptions != null)
            services.Configure(configureMQClientOptions);
        services.AddLogging();
        services.AddNClientByClientConnectionFactory<RabbitMQClientConnectionFactory>(configureClientOptions);
        services.AddSingleton<ClientConnectionCache>();
        services.AddScoped<IClientProxyProvider, RabbitMQClientProxyProvider>();
        return services;
    }

    public static IServiceCollection AddNRabbitMQGateway<TService>(this IServiceCollection services,
        Action<MQClientOptions>? configureMQClientOptions = null,
        Action<NClientOptions>? configureClientOptions = null) where TService : class
    {
        services.AddNRabbitMQClient(configureMQClientOptions, configureClientOptions);
        services.Configure<NClientOptions>(i => i.ForwardAllHeaders = true);
        services.AddNClientContract<TService>();
        services.AddNServiceContract(typeof(TService),
            p => ((ClientProxy<TService>)p.GetService(typeof(ClientProxy<TService>))!).Proxy);
        return services;
    }

    public static IServiceCollection AddNRabbitMQQueueStatus(this IServiceCollection services)
    {
        services.TryAddSingleton<QueueStatus>();
        services.TryAddSingleton<QueueStatusProvider>();
        return services;
    }
}