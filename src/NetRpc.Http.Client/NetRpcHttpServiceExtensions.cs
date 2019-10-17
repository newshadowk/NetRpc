using System;
using NetRpc;
using NetRpc.Http.Client;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class NetRpcHttpClientExtensions
    {
        public static IServiceCollection AddNetRpcHttpClient(this IServiceCollection services,
            Action<HttpClientOptions> httpClientConfigureOptions = null,
            Action<NetRpcClientOption> clientConfigureOptions = null)
        {
            if (httpClientConfigureOptions != null)
                services.Configure(httpClientConfigureOptions);
            services.AddNetRpcClientByOnceCallFactory<HttpOnceCallFactory>(clientConfigureOptions);
            services.AddSingleton<IClientProxyProvider, HttpClientProxyProvider>();
            return services;
        }
    }
}