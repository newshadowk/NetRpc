using Microsoft.Extensions.Options;
using OpenTracing;
using OpenTracing.Propagation;
using OpenTracing.Tag;

namespace NetRpc.OpenTracing;

public class ClientOpenTracingMiddleware
{
    private readonly ClientRequestDelegate _next;

    public ClientOpenTracingMiddleware(ClientRequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(ClientActionExecutingContext context, ITracer tracer, IOptions<OpenTracingOptions> options, IErrorTagHandle errorTagHandle)
    {
        if (context.ContractMethod.IsTracerIgnore)
        {
            await _next(context);
            return;
        }

        var injectDic = new Dictionary<string, string>();
        using var scope = tracer.BuildSpan($"{context.ContractMethod.MethodInfo.Name} {Const.SendStr}").StartActive(true);
        tracer.Inject(scope.Span.Context, BuiltinFormats.HttpHeaders, new TextMapInjectAdapter(injectDic));
        foreach (var dic in injectDic)
            context.Header[dic.Key] = dic.Value;

        try
        {
            await _next(context);
            scope.Span.SetTag("Connection", context.OnceCall.ConnectionInfo.ToString());
            scope.Span.SetTagReturn(context, options.Value.LogActionInfoMaxLength);
            scope.Span.SetTagMethodObj(context, options.Value.LogActionInfoMaxLength);
        }
        catch (Exception e)
        {
            scope.Span.SetTagMethodObj(context, 0, true);
            scope.Span.SetTagReturn(context, 0, true);
            scope.Span.SetTag(new StringTag("Exception"), e.ExceptionToString());
            errorTagHandle.Handle(e, scope.Span);
            throw;
        }
    }
}