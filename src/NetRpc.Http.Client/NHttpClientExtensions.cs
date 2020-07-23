using System;
using NetRpc;
using NetRpc.Http.Client;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class NHttpClientExtensions
    {
        public static IServiceCollection AddNHttpClient(this IServiceCollection services,
            Action<HttpClientOptions>? httpClientConfigureOptions = null,
            Action<NClientOption>? clientConfigureOptions = null,
            ServiceLifetime serviceLifetime = ServiceLifetime.Singleton)
        {
            if (httpClientConfigureOptions != null)
                services.Configure(httpClientConfigureOptions);
            services.AddNRpcClientByOnceCallFactory<HttpOnceCallFactory>(clientConfigureOptions, serviceLifetime);
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

            services.AddSingleton<IOrphanClientProxyProvider, OrphanHttpClientProxyProvider>();

            return services;
        }
    }
}