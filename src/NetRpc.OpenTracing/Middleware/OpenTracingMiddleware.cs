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
            if (context.ContractMethod.IsTracerIgnore)
            {
                await _next(context);
                return;
            }

            using var scope = GetScope(context, tracer, options.Value);

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
                throw;
            }
        }

        private static IScope GetScope(ActionExecutingContext context, ITracer tracer, OpenTracingOptions options)
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
            {
                scope = tracer.BuildSpan($"{context.ContractMethod.MethodInfo.Name} {ConstValue.ReceiveStr}")
                    .StartActive(true);
                context.Properties[ConstValue.IsLogSendStream] = true;
            }
            else
            {
                scope = tracer.BuildSpan($"{context.ContractMethod.MethodInfo.Name} {ConstValue.ReceiveStr}")
                    .AsChildOf(spanContext).StartActive(true);
                spanContext.CopyBaggageItemsTo(scope.Span);
                context.Properties[ConstValue.IsLogSendStream] = options.ForceLogSendStream;
            }

            return scope;
        }
    }
}