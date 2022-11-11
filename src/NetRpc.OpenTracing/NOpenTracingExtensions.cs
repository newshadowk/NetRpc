using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using NetRpc;
using NetRpc.OpenTracing;

namespace Microsoft.Extensions.DependencyInjection;

public static class NOpenTracingExtensions
{
    public static IServiceCollection AddNOpenTracing(this IServiceCollection services, Action<OpenTracingOptions>? configureOptions = null)
    {
        if (configureOptions != null)
            services.Configure(configureOptions);
        services.TryAddSingleton<IGlobalTracerAccessor, GlobalTracerAccessor>();
        services.Configure<ClientMiddlewareOptions>(i =>
        {
            i.UseMiddleware<ClientOpenTracingMiddleware>();
            i.UseMiddleware<ClientStreamTracingMiddleware>();
        });
        services.Configure<MiddlewareOptions>(i =>
        {
            i.UseMiddleware<OpenTracingMiddleware>();
            i.UseMiddleware<StreamTracingMiddleware>();
        });
        services.AddSingleton<ILoggerProvider, SpanLoggerProvider>();
        services.AddSingleton<IErrorTagHandle, DefaultErrorTagHandle>();
        return services;
    }
}