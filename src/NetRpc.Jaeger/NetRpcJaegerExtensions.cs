using System;
using Jaeger;
using Jaeger.Reporters;
using Jaeger.Samplers;
using Jaeger.Senders;
using Microsoft.Extensions.Options;
using NetRpc;
using NetRpc.Jaeger;
using OpenTracing;
using OpenTracing.Util;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class NetRpcJaegerExtensions
    {
        public static IServiceCollection AddNetRpcJaeger(this IServiceCollection services, Action<JaegerOptions> configureOptions)
        {
            services.Configure(configureOptions);
            services.Configure<MiddlewareOptions>(i => i.UseMiddleware<NetRpcServiceJaegerMiddleware>());
            services.Configure<ClientMiddlewareOptions>(i => i.UseMiddleware<NetRpcClientJaegerMiddleware>());
            services.AddNetRpcOpenTracing();
            services.AddSingleton(typeof(ITracer), i =>
            {
                var opt = i.GetService<IOptions<JaegerOptions>>();
                var tracer = GetTracer(opt.Value);
                GlobalTracer.Register(tracer);
                return tracer;
            });

            return services;
        }

        private static Tracer GetTracer(JaegerOptions options)
        {
            var sampler = new ConstSampler(true);
            var reporter = new RemoteReporter.Builder()
                //.WithMaxQueueSize(...)            // optional, defaults to 100
                //.WithFlushInterval(...)           // optional, defaults to TimeSpan.FromSeconds(1)
                .WithSender(new UdpSender(options.Host, options.Port, 0)) // optional, defaults to UdpSender("localhost", 6831, 0)
                .Build();

            // This will log to a default localhost installation of Jaeger.
            var tracer = new Tracer.Builder(options.ServiceName)
                .WithSampler(sampler)
                .WithReporter(reporter)
                .Build();

            return tracer;
        }
    }
}