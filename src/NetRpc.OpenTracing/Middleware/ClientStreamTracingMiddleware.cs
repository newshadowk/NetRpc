using System.Threading.Tasks;
using OpenTracing;
using OpenTracing.Util;

namespace NetRpc.OpenTracing
{
    public class ClientStreamTracingMiddleware
    {
        private readonly ClientRequestDelegate _next;

        public ClientStreamTracingMiddleware(ClientRequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(ClientActionExecutingContext context)
        {
            SetTracingBefore(context);
            await _next(context);
            SetTracingAfter(context);
        }

        private static void SetTracingBefore(ClientActionExecutingContext context)
        {
            var spanBuilder = GlobalTracer.Instance.BuildSpan($"{ConstValue.ClientStream} {ConstValue.SendStr}").AsChildOf(GlobalTracer.Instance.ActiveSpan);
            ISpan span = null;
            context.OnceCall.SendRequestStreamStarted += (s, e) => span = spanBuilder.Start();
            context.OnceCall.SendRequestStreamFinished += (s, e) => span?.Finish();
        }

        private static void SetTracingAfter(ClientActionExecutingContext context)
        {
            if (context.Result.TryGetStream(out var outStream, out _))
            {
                var bbs = (BufferBlockStream)outStream;
                var spanBuilder = GlobalTracer.Instance.BuildSpan($"{ConstValue.ClientStream} {ConstValue.ReceiveStr}").AsChildOf(GlobalTracer.Instance.ActiveSpan);
                ISpan span = null;
                bbs.Started += (s, e) => span = spanBuilder.Start();
                bbs.Finished += (s, e) => span?.Finish();
            }
        }
    }
}