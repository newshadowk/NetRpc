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

            var span = tracer.BuildSpan($"{context.ContractMethod.MethodInfo.Name} {ConstValue.SendStr}").Start();

            //header
            bool isLogDetails = Helper.GetIsLogDetails(opt);
            var injectDic = new Dictionary<string, string>();
            if (isLogDetails)
                tracer.Inject(span.Context, BuiltinFormats.HttpHeaders, new TextMapInjectAdapter(injectDic));
            else
                tracer.Inject(GlobalTracer.Instance.ScopeManager.Active.Span.Context, BuiltinFormats.HttpHeaders, new TextMapInjectAdapter(injectDic));
            foreach (var dic in injectDic)
                context.Header.Add(dic.Key, dic.Value);

            try
            {
                await _next(context);
                if (isLogDetails)
                {
                    span.SetTagReturn(context, opt.LogActionInfoMaxLength);
                    span.SetTagMethodObj(context, opt.LogActionInfoMaxLength);
                    span.Finish();
                }
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
    }
}