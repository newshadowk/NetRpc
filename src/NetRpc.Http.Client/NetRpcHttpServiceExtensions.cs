using System;
using NetRpc;
using NetRpc.Http.Client;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class NetRpcHttpClientExtensions
    {
        public static IServiceCollection AddNetRpcHttpClient(this IServiceCollection services,
            Action<HttpClientOptions> httpClientConfigureOptions = null,
            Action<NetRpcClientOption> clientConfigureOptions = null,
            ServiceLifetime serviceLifetime = ServiceLifetime.Singleton)
        {
            if (httpClientConfigureOptions != null)
                services.Configure(httpClientConfigureOptions);
            services.AddNetRpcClientByOnceCallFactory<HttpOnceCallFactory>(clientConfigureOptions, serviceLifetime);
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
}