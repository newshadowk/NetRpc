using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using OpenTracing;
using OpenTracing.Propagation;
using OpenTracing.Tag;

namespace NetRpc.OpenTracing
{
    public class ClientOpenTracingMiddleware
    {
        private readonly ClientRequestDelegate _next;

        public ClientOpenTracingMiddleware(ClientRequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(ClientActionExecutingContext context, ITracer tracer, IOptions<OpenTracingOptions> options)
        {
            using var scope = tracer.BuildSpan($"{context.ContractMethod.MethodInfo.Name} {ConstValue.SendStr}").StartActive(true);

            var injectDic = new Dictionary<string, string>();
            tracer.Inject(scope.Span.Context, BuiltinFormats.HttpHeaders, new TextMapInjectAdapter(injectDic));

            scope.Span.SetTagMethodObj(context, options.Value.LogActionInfoMaxLength);

            if (context.Header == null)
                context.Header = new Dictionary<string, object>();

            foreach (var dic in injectDic)
                context.Header.Add(dic.Key, dic.Value);

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
    }
}