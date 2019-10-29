using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OpenTracing;
using OpenTracing.Propagation;
using OpenTracing.Tag;

namespace NetRpc.OpenTracing
{
    public class NetRpcOpenTracingMiddleware
    {
        private readonly RequestDelegate _next;

        public NetRpcOpenTracingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(ActionExecutingContext context, ITracer tracer)
        {
            IScope scope;
            if (context.Header != null && context.Header.ContainsKey("uber-trace-id"))
            {
                var spanContext = tracer.Extract(BuiltinFormats.HttpHeaders, new RequestHeadersExtractAdapter(context.Header));
                scope = tracer.BuildSpan(context.ContractMethod.MethodInfo.Name)
                    .AsChildOf(spanContext)
                    .StartActive(true);
            }
            else
            {
                scope = tracer.BuildSpan(context.ContractMethod.MethodInfo.Name).StartActive(true);
            }

            using (scope)
            {
                if (!context.ContractMethod.IsTraceArgsIgnore)
                    scope.Span.SetTagMethodObj(context.ContractMethod, context.PureArgs);

                try
                {
                    await _next(context);
                    if (!context.ContractMethod.IsTraceReturnIgnore)
                        scope.Span.SetTag(new StringTag("Result"), context.Result.ToDtoJson());
                }
                catch (Exception e)
                {
                    var str = Helper.GetException(e);
                    scope.Span.SetTag(new StringTag("Exception"), str);
                    throw;
                }
            }
        }
    }

    public class NetRpcClientOpenTracingMiddleware
    {
        private readonly ClientRequestDelegate _next;

        public NetRpcClientOpenTracingMiddleware(ClientRequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(ClientActionExecutingContext context, ITracer tracer)
        {
            using (var scope = tracer.BuildSpan(context.ContractMethod.MethodInfo.Name).StartActive(true))
            {
                var injectDic = new Dictionary<string, string>();
                tracer.Inject(scope.Span.Context, BuiltinFormats.HttpHeaders, new TextMapInjectAdapter(injectDic));

                if (!context.ContractMethod.IsTraceArgsIgnore)
                    scope.Span.SetTagMethodObj(context.ContractMethod, context.PureArgs);

                if (context.Header == null) 
                    context.Header = new Dictionary<string, object>();

                foreach (var dic in injectDic) 
                    context.Header.Add(dic.Key, dic.Value);

                try
                {
                    await _next(context);

                    if (!context.ContractMethod.IsTraceReturnIgnore)
                        scope.Span.SetTag(new StringTag("Result"), context.Result.ToDtoJson());
                }
                catch (Exception e)
                {
                    var str = Helper.GetException(e);
                    scope.Span.SetTag(new StringTag("Exception"), str);
                    throw;
                }
            }
        }
    }
}