using System;
using NetRpc;
using NetRpc.Http.Client;

namespace Microsoft.Extensions.DependencyInjection
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