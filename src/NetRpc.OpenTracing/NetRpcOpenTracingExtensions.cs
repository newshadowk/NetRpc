using Microsoft.Extensions.DependencyInjection.Extensions;
using NetRpc;
using NetRpc.OpenTracing;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class NetRpcOpenTracingExtensions
    {
        public static IServiceCollection AddNetRpcOpenTracing(this IServiceCollection services)
        {
            services.TryAddSingleton<IGlobalTracerAccessor, GlobalTracerAccessor>();
            services.Configure<MiddlewareOptions>(i => i.UseMiddleware<NetRpcOpenTracingMiddleware>());
            services.Configure<ClientMiddlewareOptions>(i => i.UseMiddleware<NetRpcClientOpenTracingMiddleware>());
            return services;
        }
    }
}