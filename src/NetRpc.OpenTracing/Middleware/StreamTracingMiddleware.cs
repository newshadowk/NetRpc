using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using OpenTracing;
using OpenTracing.Util;

namespace NetRpc.OpenTracing
{
    public class StreamTracingMiddleware
    {
        private readonly RequestDelegate _next;

        public StreamTracingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(ActionExecutingContext context, IOptions<OpenTracingOptions> options)
        {
            bool isLogDetails = Helper.GetIsLogDetails(options.Value);
            var currSpan = GlobalTracer.Instance.ActiveSpan;
            SetTracingBefore(context, currSpan);
            await _next(context);
            if (isLogDetails)
                SetTracingAfter(context);
        }

        private static void SetTracingBefore(ActionExecutingContext context, ISpan currSpan)
        {
            if (!(context.Stream is BufferBlockStream))
                return;

            var bbs = (BufferBlockStream)context.Stream;
            var spanBuilder = GlobalTracer.Instance.BuildSpan($"{context.ContractMethod.MethodInfo.Name} {ConstValue.ServiceStream} {ConstValue.ReceiveStr}").AsChildOf(currSpan);
            ISpan span = null;
            bbs.Started += (s, e) => span = spanBuilder.Start();
            bbs.Finished += (s, e) => { span?.Finish(); };
        }

        private static void SetTracingAfter(ActionExecutingContext context)
        {
            var spanBuilder = GlobalTracer.Instance.BuildSpan($"{context.ContractMethod.MethodInfo.Name} {ConstValue.ServiceStream} {ConstValue.SendStr}")
                .AsChildOf(GlobalTracer.Instance.ActiveSpan);
            ISpan span = null;
            context.SendResultStreamStarted += (s, e) => span = spanBuilder.Start();
            context.SendResultStreamFinished += (s, e) => span?.Finish();
        }
    }
}