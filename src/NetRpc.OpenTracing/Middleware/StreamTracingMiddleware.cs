using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using OpenTracing;
using OpenTracing.Util;

namespace NetRpc.OpenTracing;

public class StreamTracingMiddleware
{
    private readonly RequestDelegate _next;

    public StreamTracingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(ActionExecutingContext context)
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

    private static void SetTracingBefore(ActionExecutingContext context)
    {
        if (context.Stream == null || context.Stream.Length == 0 || GlobalTracer.Instance.ActiveSpan == null)
            return;

        var spanBuilder = GlobalTracer.Instance.BuildSpan(
                $"{Const.ServiceStream} {NetRpc.Helper.SizeSuffix(context.Stream.Length)} {Const.ReceiveStr}")
            .AsChildOf(GlobalTracer.Instance.ActiveSpan);
        ISpan? span = null;
#pragma warning disable 1998
        context.Stream.StartedAsync += async (_, _) => span = spanBuilder.Start();
        context.Stream.FinishedAsync += async (_, _) => { span?.Finish(); };
#pragma warning restore 1998
    }

    private static void SetTracingAfter(ActionExecutingContext context)
    {
        if (GlobalTracer.Instance.ActiveSpan == null)
            return;

        if (context.Result.TryGetStream(out var outStream, out _))
        {
            var spanBuilder = GlobalTracer.Instance
                .BuildSpan($"{Const.ServiceStream} {NetRpc.Helper.SizeSuffix(outStream!.Length)} {Const.SendStr}")
                .AsChildOf(GlobalTracer.Instance.ActiveSpan);
            ISpan? span = null;
            context.SendResultStreamStarted += (_, _) => span = spanBuilder.Start();
            context.SendResultStreamEndOrFault += (_, _) => span?.Finish();
        }
    }
}