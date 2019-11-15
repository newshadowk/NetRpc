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
            if (context.ContractMethod.IsTracerIgnore)
            {
                await _next(context);
                return;
            }

            bool isLogDetails = Helper.GetIsLogDetails(options.Value);
            SetTracingBefore(context);
            await _next(context);

            var isRoot = (bool)context.Properties[ConstValue.IsLogSendStream];
            if (isLogDetails || isRoot)
                SetTracingAfter(context);
        }

        private static void SetTracingBefore(ActionExecutingContext context)
        {
            if (!(context.Stream is BufferBlockStream) || GlobalTracer.Instance.ActiveSpan == null)
                return;

            var bbs = (BufferBlockStream)context.Stream;
            var spanBuilder = GlobalTracer.Instance.BuildSpan(
                $"{ConstValue.ServiceStream} {Helper.SizeSuffix(bbs.Length)} {ConstValue.ReceiveStr}").AsChildOf(GlobalTracer.Instance.ActiveSpan);
            ISpan span = null;
            bbs.Started += (s, e) => span = spanBuilder.Start();
            bbs.Finished += (s, e) => { span?.Finish(); };
        }

        private static void SetTracingAfter(ActionExecutingContext context)
        {
            if (GlobalTracer.Instance.ActiveSpan == null)
                return;

            if (context.Result.TryGetStream(out var outStream, out _))
            {
                var spanBuilder = GlobalTracer.Instance.BuildSpan($"{ConstValue.ServiceStream} {Helper.SizeSuffix(outStream.Length)} {ConstValue.SendStr}")
                    .AsChildOf(GlobalTracer.Instance.ActiveSpan);
                ISpan span = null;
                context.SendResultStreamStarted += (s, e) => span = spanBuilder.Start();
                context.SendResultStreamFinished += (s, e) => span?.Finish();
            }
        }
    }
}