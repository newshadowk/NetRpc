using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetRpc.Contract;

namespace NetRpc.Http;

internal class NHttpMiddleware
{
    private readonly Microsoft.AspNetCore.Http.RequestDelegate _next;

    public NHttpMiddleware(Microsoft.AspNetCore.Http.RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext httpContext, IOptions<ContractOptions> contractOptions, IHubContext<CallbackHub, ICallback> hub,
        IOptions<HttpServiceOptions> httpOptions, RequestHandler requestHandler, HttpObjProcessorManager httpObjProcessorManager,
        ILoggerFactory loggerFactory)
    {
        //if grpc channel message go to next.
        if (httpContext.Request.Path.Value!.EndsWith("DuplexStreamingServerMethod"))
        {
            await _next(httpContext);
            return;
        }

        bool notMatched;
        await using (var convert = new HttpServiceOnceApiConvert(contractOptions.Value.Contracts, httpContext, httpOptions.Value.ApiRootPath,
            httpOptions.Value.IgnoreWhenNotMatched, hub, httpObjProcessorManager, loggerFactory))
        {
            await requestHandler.HandleAsync(convert, ChannelType.Http);
            notMatched = convert.NotMatched;
        }

        if (httpOptions.Value.IgnoreWhenNotMatched && notMatched)
            await _next(httpContext);
    }
}