using Microsoft.Extensions.Options;
using OpenTracing;
using OpenTracing.Tag;

namespace NetRpc.Jaeger;

public class ServiceJaegerMiddleware
{
    private readonly RequestDelegate _next;

    public ServiceJaegerMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(ActionExecutingContext context, ITracer tracer, IOptions<ServiceSwaggerOptions> options)
    {
        if (options.Value.IsPropertiesDefault())
        {
            await _next(context);
            return;
        }

        //http://localhost:5001/swagger/index.html#/IService/post_IService_Call
        var info = context.ContractMethod.Route.DefaultRout;
        var requestUrl = Helper.GetRequestUrl(options.Value.HostPath.FormatUrl(), options.Value.ApiPath, info.ContractTag, info.Path);
        tracer.ActiveSpan?.SetTag(new StringTag("Url"), requestUrl);

        await _next(context);
    }
}

public class ClientJaegerMiddleware
{
    private readonly ClientRequestDelegate _next;

    public ClientJaegerMiddleware(ClientRequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(ClientActionExecutingContext context, ITracer tracer, IOptionsMonitor<ClientSwaggerOptions> options)
    {
        var opt = options.Get(context.OptionsName);

        if (opt.IsPropertiesDefault())
        {
            await _next(context);
            return;
        }

        var info = context.ContractMethod.Route.DefaultRout;
        var requestUrl = Helper.GetRequestUrl(opt.HostPath.FormatUrl(), opt.ApiPath, info.ContractTag, info.Path);
        tracer.ActiveSpan?.SetTag(new StringTag("Url"), requestUrl);

        await _next(context);
    }
}