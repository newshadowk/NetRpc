using System.Threading.Tasks;
using OpenTracing;
using OpenTracing.Util;

namespace NetRpc.OpenTracing;

public class ClientStreamTracingMiddleware
{
    private readonly ClientRequestDelegate _next;

    public ClientStreamTracingMiddleware(ClientRequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(ClientActionExecutingContext context)
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
                $"{Const.ClientStream} {NetRpc.Helper.SizeSuffix(context.Stream.GetLength())} {Const.SendStr}")
            .AsChildOf(GlobalTracer.Instance.ActiveSpan);
        ISpan? span = null;
        context.OnceCall.SendRequestStreamStarted += (_, _) => span = spanBuilder.Start();
        context.OnceCall.SendRequestStreamEndOrFault += (_, _) => span?.Finish();
    }

    private static void SetTracingAfter(ClientActionExecutingContext context)
    {
        if (GlobalTracer.Instance.ActiveSpan == null)
            return;

        if (context.Result.TryGetStream(out var outStream, out _))
        {
            var readStream = (ProxyStream) outStream;
            var spanBuilder = GlobalTracer.Instance.BuildSpan(
                    $"{Const.ClientStream} {NetRpc.Helper.SizeSuffix(readStream.GetLength())} {Const.ReceiveStr}")
                .AsChildOf(GlobalTracer.Instance.ActiveSpan);
            ISpan? span = null;

#pragma warning disable 1998
            readStream.StartedAsync += async (_, _) => span = spanBuilder.Start();
            readStream.FinishedAsync += async (_, _) => span?.Finish();
#pragma warning restore 1998
        }
    }
}