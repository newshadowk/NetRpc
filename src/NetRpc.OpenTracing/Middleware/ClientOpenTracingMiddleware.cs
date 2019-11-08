using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using OpenTracing;
using OpenTracing.Propagation;
using OpenTracing.Tag;
using OpenTracing.Util;

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
            var opt = options.Value;
            if (Helper.GetIsLogDetails(opt))
                await InvokeAsyncByLogDetailsAsync(context, tracer, opt);
            else
                await InvokeAsyncByNotLogDetailsAsync(context, tracer, opt);
        }

        private async Task InvokeAsyncByNotLogDetailsAsync(ClientActionExecutingContext context, ITracer tracer, OpenTracingOptions options)
        {
            //header
            var injectDic = new Dictionary<string, string>();

            tracer.Inject(GlobalTracer.Instance.ScopeManager.Active.Span.Context, BuiltinFormats.HttpHeaders, new TextMapInjectAdapter(injectDic));
            foreach (var dic in injectDic)
                context.Header.Add(dic.Key, dic.Value);

            var span = tracer.BuildSpan($"{context.ContractMethod.MethodInfo.Name} {ConstValue.SendStr}").Start();
            SetIsLogDetails(span, options);

            try
            {
                await _next(context);
            }
            catch (Exception e)
            {
                span.SetTagMethodObj(context, 0, true);
                span.SetTagReturn(context, 0, true);
                span.SetTag(new StringTag("Exception"), Helper.GetException(e));
                span.Finish();
                throw;
            }
        }

        private async Task InvokeAsyncByLogDetailsAsync(ClientActionExecutingContext context, ITracer tracer, OpenTracingOptions options)
        {
            var injectDic = new Dictionary<string, string>();
            using var scope = tracer.BuildSpan($"{context.ContractMethod.MethodInfo.Name} {ConstValue.SendStr}").StartActive(true);
            SetIsLogDetails(scope.Span, options);
            tracer.Inject(scope.Span.Context, BuiltinFormats.HttpHeaders, new TextMapInjectAdapter(injectDic));
            foreach (var dic in injectDic)
                context.Header.Add(dic.Key, dic.Value);

            try
            {
                await _next(context);
                scope.Span.SetTagReturn(context, options.LogActionInfoMaxLength);
                scope.Span.SetTagMethodObj(context, options.LogActionInfoMaxLength);
            }
            catch (Exception e)
            {
                scope.Span.SetTagMethodObj(context, 0, true);
                scope.Span.SetTagReturn(context, 0, true);
                scope.Span.SetTag(new StringTag("Exception"), Helper.GetException(e));
                throw;
            }
        }

        private static void SetIsLogDetails(ISpan span, OpenTracingOptions options)
        {
            if (span.GetBaggageItem(ConstValue.IsLogDetails) == null) 
                span.SetBaggageItem(ConstValue.IsLogDetails, options.IsLogDetails.ToString());
        }
    }
}