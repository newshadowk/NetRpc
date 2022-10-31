using System;
using NetRpc;
using NetRpc.Http.Client;

namespace Microsoft.Extensions.DependencyInjection;

public static class NHttpClientExtensions
{
    public static IServiceCollection AddNHttpClient(this IServiceCollection services,
        Action<HttpClientOptions>? configureHttpClientOptions = null,
        Action<NClientOptions>? configureClientOptions = null,
        ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
    {
        if (configureHttpClientOptions != null)
            services.Configure(configureHttpClientOptions);
        services.AddLogging();
        services.AddNClientByOnceCallFactory<HttpOnceCallFactory>(configureClientOptions, serviceLifetime);
        switch (serviceLifetime)
        {
            case ServiceLifetime.Singleton:
                services.AddSingleton<IClientProxyProvider, HttpClientProxyProvider>();
                break;
            case ServiceLifetime.Scoped:
                services.AddScoped<IClientProxyProvider, HttpClientProxyProvider>();
                break;
            case ServiceLifetime.Transient:
                services.AddTransient<IClientProxyProvider, HttpClientProxyProvider>();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(serviceLifetime), serviceLifetime, null);
        }

        return services;
    }
}