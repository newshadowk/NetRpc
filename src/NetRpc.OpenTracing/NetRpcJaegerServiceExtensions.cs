using Microsoft.Extensions.DependencyInjection.Extensions;
using NetRpc;
using NetRpc.OpenTracing;
using OpenTracing.Util;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class NetRpcOpenTracingExtensions
    {
        public static IServiceCollection AddNetRpcOpenTracing(this IServiceCollection services)
        {
            services.TryAddSingleton(GlobalTracer.Instance);
            services.TryAddSingleton<IGlobalTracerAccessor, GlobalTracerAccessor>();
            services.Configure<MiddlewareOptions>(i => i.UseMiddleware<NetRpcOpenTracingMiddleware>());
            return services;
        }
    }
}