using System;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NetRpc;
using NetRpc.OpenTracing;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class NetRpcOpenTracingExtensions
    {
        public static IServiceCollection AddNetRpcOpenTracing(this IServiceCollection services, Action<OpenTracingOptions> configureOptions = null)
        {
            if (configureOptions != null)
                services.Configure(configureOptions);
            services.TryAddSingleton<IGlobalTracerAccessor, GlobalTracerAccessor>();
            services.Configure<MiddlewareOptions>(i =>
            {
                i.UseMiddleware<StreamTracingMiddleware>();
                i.UseMiddleware<OpenTracingMiddleware>();
            });
            services.Configure<ClientMiddlewareOptions>(i =>
            {
                i.UseMiddleware<ClientStreamTracingMiddleware>();
                i.UseMiddleware<ClientOpenTracingMiddleware>();
            });
            return services;
        }
    }
}