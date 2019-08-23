using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace NetRpc.Http
{
    public class HttpNetRpcMiddleware
    {
        private readonly Microsoft.AspNetCore.Http.RequestDelegate _next;

        public HttpNetRpcMiddleware(Microsoft.AspNetCore.Http.RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext httpContext, IOptionsSnapshot<ContractOptions> contractOptions, IHubContext<CallbackHub, ICallback> hub, IOptionsSnapshot<HttpServiceOptions> httpOptions, RequestHandler requestHandler)
        {
            bool notMatched;
            using (var convert = new NetRpcHttpServiceOnceApiConvert(contractOptions.Value.InstanceTypes, httpContext,
                httpOptions.Value.ApiRootPath, httpOptions.Value.IgnoreWhenNotMatched, httpOptions.Value.SupportCallbackAndCancel, hub, httpOptions.Value.IsClearStackTrace))
            {
                await requestHandler.HandleHttpAsync(convert);
                notMatched = convert.NotMatched;
            }

            if (httpOptions.Value.IgnoreWhenNotMatched && notMatched)
                await _next(httpContext);
        }
    }
}