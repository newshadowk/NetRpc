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

        public async Task InvokeAsync(ActionExecutingContext context, ITracer tracer, IOptions<OpenTracingOptions> options, IErrorTagHandle errorTagHandle)
        {
            if (context.ContractMethod.IsTracerIgnore)
            {
                await _next(context);
                return;
            }

            using var scope = GetScope(context, tracer);

            try
            {
                await _next(context);
                scope.Span.SetTagMethodObj(context, options.Value.LogActionInfoMaxLength);
                scope.Span.SetTagReturn(context, options.Value.LogActionInfoMaxLength);
            }
            catch (Exception e)
            {
                scope.Span.SetTagMethodObj(context, options.Value.LogActionInfoMaxLength, true);
                scope.Span.SetTagReturn(context, options.Value.LogActionInfoMaxLength, true);
                scope.Span.SetTag(new StringTag("Exception"), e.ExceptionToString());
                errorTagHandle.Handle(e, scope.Span);
                throw;
            }
        }

        private static IScope GetScope(ActionExecutingContext context, ITracer tracer)
        {
            //get spanContext
            ISpanContext? spanContext;
            if (context.Headers.Count == 0)
                spanContext = null;
            else
                spanContext = tracer.Extract(BuiltinFormats.HttpHeaders, new RequestHeadersExtractAdapter(context.Headers));

            //get scope
            IScope scope;
            if (spanContext == null)
            {
                scope = tracer.BuildSpan($"{context.ContractMethod.MethodInfo.Name} {ConstValue.ReceiveStr}")
                    .StartActive(true);
            }
            else
            {
                scope = tracer.BuildSpan($"{context.ContractMethod.MethodInfo.Name} {ConstValue.ReceiveStr}")
                    .AsChildOf(spanContext).StartActive(true);
                spanContext.CopyBaggageItemsTo(scope.Span);
            }

            return scope;
        }
    }
}