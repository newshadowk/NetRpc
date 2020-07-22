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
            Console.WriteLine("StreamTracingMiddleware");
            if (context.ContractMethod.IsTracerIgnore)
            {
                await _next(context);
                return;
            }

            SetTracingBefore(context);
            await _next(context);
            SetTracingAfter(context);
        }

        private static void SetTracingBefore(ActionExecutingContext context)
        {
            if (context.Stream == null || context.Stream.Length == 0 || GlobalTracer.Instance.ActiveSpan == null)
                return;

            var spanBuilder = GlobalTracer.Instance.BuildSpan(
                $"{ConstValue.ServiceStream} {NetRpc.Helper.SizeSuffix(context.Stream.Length)} {ConstValue.ReceiveStr}").AsChildOf(GlobalTracer.Instance.ActiveSpan);
            ISpan? span = null;
#pragma warning disable 1998
            context.Stream.StartedAsync += async (s, e) => span = spanBuilder.Start();
            context.Stream.FinishedAsync += async (s, e) => { span?.Finish(); };
#pragma warning restore 1998
        }

        private static void SetTracingAfter(ActionExecutingContext context)
        {
            if (GlobalTracer.Instance.ActiveSpan == null)
                return;

            if (context.Result.TryGetStream(out var outStream, out _))
            {
                var spanBuilder = GlobalTracer.Instance.BuildSpan($"{ConstValue.ServiceStream} {NetRpc.Helper.SizeSuffix(outStream!.Length)} {ConstValue.SendStr}")
                    .AsChildOf(GlobalTracer.Instance.ActiveSpan);
                ISpan? span = null;
                context.SendResultStreamStarted += (s, e) => span = spanBuilder.Start();
                context.SendResultStreamFinished += (s, e) => span?.Finish();
            }
        }
    }
}