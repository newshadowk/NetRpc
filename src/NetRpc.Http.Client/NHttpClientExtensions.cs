using NetRpc;
using NetRpc.Http.Client;

namespace Microsoft.Extensions.DependencyInjection;

public static class NHttpClientExtensions
{
    public static IServiceCollection AddNHttpClient(this IServiceCollection services,
        Action<HttpClientOptions>? configureHttpClientOptions = null,
        Action<NClientOptions>? configureClientOptions = null)
    {
        if (configureHttpClientOptions != null)
            services.Configure(configureHttpClientOptions);
        services.AddLogging();
        services.AddNClientByOnceCallFactory<HttpOnceCallFactory>(configureClientOptions);
        services.AddScoped<IClientProxyProvider, HttpClientProxyProvider>();
        return services;
    }
}