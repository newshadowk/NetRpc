using System.Threading.Tasks;
using Microsoft.Extensions.Options;
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

        public async Task InvokeAsync(ClientActionExecutingContext context, IOptions<OpenTracingOptions> options)
        {
            if (context.ContractMethod.IsTracerIgnore)
            {
                await _next(context);
                return;
            }

            SetTracingBefore(context);
            await _next(context);
            SetTracingAfter(context);
        }

        private static void SetTracingBefore(ClientActionExecutingContext context)
        {
            if (context.Stream == null || GlobalTracer.Instance.ActiveSpan == null)
                return;

            var spanBuilder = GlobalTracer.Instance.BuildSpan(
                    $"{ConstValue.ClientStream} {NetRpc.Helper.SizeSuffix(context.Stream.Length)} {ConstValue.SendStr}")
                .AsChildOf(GlobalTracer.Instance.ActiveSpan);
            ISpan? span = null;
            context.OnceCall.SendRequestStreamStarted += (s, e) => span = spanBuilder.Start();
            context.OnceCall.SendRequestStreamFinished += (s, e) => span?.Finish();
        }

        private static void SetTracingAfter(ClientActionExecutingContext context)
        {
            if (GlobalTracer.Instance.ActiveSpan == null)
                return;

            if (context.Result.TryGetStream(out var outStream, out _))
            {
                var readStream = (ReadStream) outStream!;
                var spanBuilder = GlobalTracer.Instance.BuildSpan(
                        $"{ConstValue.ClientStream} {NetRpc.Helper.SizeSuffix(readStream.Length)} {ConstValue.ReceiveStr}")
                    .AsChildOf(GlobalTracer.Instance.ActiveSpan);
                ISpan? span = null;

#pragma warning disable 1998
                readStream.StartedAsync += async (s, e) => span = spanBuilder.Start();
                readStream.FinishedAsync += async (s, e) => span?.Finish();
#pragma warning restore 1998
            }
        }
    }
}