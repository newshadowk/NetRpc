using System;
using Microsoft.Extensions.DependencyInjection;

namespace NetRpc.Http.Client
{
    public static class NetRpcHttpClientExtensions
    {
        public static IServiceCollection AddNetRpcHttpClient<TService>(this IServiceCollection services,
            Action<HttpClientOptions> httpClientConfigureOptions = null,
            Action<NetRpcClientOption> clientConfigureOptions = null)
        {
            if (httpClientConfigureOptions != null)
                services.Configure(httpClientConfigureOptions);
            services.AddNetRpcClientByApiConvert<HttpOnceCallFactory, TService>(clientConfigureOptions);
            return services;
        }
    }
}