using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using OpenTracing;
using OpenTracing.Propagation;
using OpenTracing.Tag;

namespace NetRpc.OpenTracing
{
    public class OpenTracingMiddleware
    {
        private readonly RequestDelegate _next;

        public OpenTracingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(ActionExecutingContext context, ITracer tracer, IOptions<OpenTracingOptions> options)
        {
            using var scope = GetScope(context, tracer);

            scope.Span.SetTagMethodObj(context, options.Value.LogActionInfoMaxLength);

            try
            {
                await _next(context);
                scope.Span.SetTagReturn(context, options.Value.LogActionInfoMaxLength);
            }
            catch (Exception e)
            {
                if (context.ContractMethod.IsTraceArgsIgnore)
                    scope.Span.SetTagMethodObj(context, options.Value.LogActionInfoMaxLength, true);

                if (context.ContractMethod.IsTraceReturnIgnore)
                    scope.Span.SetTagReturn(context, options.Value.LogActionInfoMaxLength, true);

                var str = Helper.GetException(e);
                scope.Span.SetTag(new StringTag("Exception"), str);
                throw;
            }
        }

        private static IScope GetScope(ActionExecutingContext context, ITracer tracer)
        {
            //get spanContext
            ISpanContext spanContext;
            if (context.Header == null)
                spanContext = null;
            else
                spanContext = tracer.Extract(BuiltinFormats.HttpHeaders, new RequestHeadersExtractAdapter(context.Header));

            //get scope
            IScope scope;
            if (spanContext == null)
                scope = tracer.BuildSpan($"{context.ContractMethod.MethodInfo.Name} {ConstValue.ReceiveStr}").StartActive(true);
            else
            {
                scope = tracer.BuildSpan($"{context.ContractMethod.MethodInfo.Name} {ConstValue.ReceiveStr}")
                    .AsChildOf(spanContext)
                    .StartActive(true);
            }

            return scope;
        }
    }
}