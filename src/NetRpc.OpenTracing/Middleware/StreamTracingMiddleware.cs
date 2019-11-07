using System.IO;
using System.Threading.Tasks;
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

        public async Task InvokeAsync(ActionExecutingContext context)
        {
            SetTracingBefore(context);
            await _next(context);
            SetTracingAfter(context);
        }

        private static void SetTracingBefore(ActionExecutingContext context)
        {
            if (!(context.Stream is BufferBlockStream))
                return;

            var bbs = (BufferBlockStream)context.Stream;

            var spanBuilder = GlobalTracer.Instance.BuildSpan($"{ConstValue.ServiceStream} {ConstValue.ReceiveStr}").AsChildOf(GlobalTracer.Instance.ActiveSpan);
            ISpan span = null;
            bbs.Started += (s, e) => span = spanBuilder.Start();
            bbs.Finished += (s, e) => { span?.Finish(); };
        }

        private static void SetTracingAfter(ActionExecutingContext context)
        {
            var spanBuilder = GlobalTracer.Instance.BuildSpan($"{ConstValue.ServiceStream} {ConstValue.SendStr}").AsChildOf(GlobalTracer.Instance.ActiveSpan);
            ISpan span = null;
            context.ResultStreamStarted += (s, e) => span = spanBuilder.Start();
            context.ResultStreamFinished += (s, e) => span?.Finish();
        }
    }
}