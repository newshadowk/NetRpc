using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace NetRpc.Http
{
    public class HttpNetRpcMiddleware
    {
        private readonly Microsoft.AspNetCore.Http.RequestDelegate _next;
        private readonly string _rootPath;
        private readonly RequestHandler _requestHandler;
        private readonly bool _ignoreWhenNotMatched;

        public HttpNetRpcMiddleware(Microsoft.AspNetCore.Http.RequestDelegate next, string rootPath, RequestHandler requestHandler, bool ignoreWhenNotMatched = false)
        {
            _next = next;
            _rootPath = rootPath;
            _requestHandler = requestHandler;
            _ignoreWhenNotMatched = ignoreWhenNotMatched;
        }

        public async Task Invoke(HttpContext httpContext, IHubContext<CallbackHub, ICallback> hub)
        {
            bool notMatched;
            using (var convert = new HttpNetRpcServiceOnceApiConvert(httpContext, _rootPath, _ignoreWhenNotMatched, hub, _requestHandler.Instances))
            {
                await _requestHandler.HandleAsync(convert);
                notMatched = convert.NotMatched;
            }

            if (_ignoreWhenNotMatched && notMatched)
                await _next(httpContext);
        }
    }
}