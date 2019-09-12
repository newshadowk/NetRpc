using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using NetRpc.Http.Client;

namespace NetRpc.Http
{
    internal sealed class HttpConnection
    {
        private readonly HttpContext _context;
        private readonly IHubContext<CallbackHub, ICallback> _hub;

        public HttpConnection(HttpContext context, IHubContext<CallbackHub, ICallback> hub)
        {
            _context = context;
            _hub = hub;
        }

        public string ConnectionId { get; set; }

        public string CallId { get; set; }

        public async Task CallBack(object callbackObj)
        {
            try
            {
                if (ConnectionId != null)
                    await _hub.Clients.Client(ConnectionId).Callback(CallId, callbackObj.ToJson());
            }
            catch
            {
            }
        }

        public async Task SendWithStreamAsync(CustomResult result, Stream stream, string streamName)
        {
            var emptyActionDescriptor = new ActionDescriptor();
            var routeData = _context.GetRouteData() ?? new RouteData();
            var actionContext = new ActionContext(_context, routeData, emptyActionDescriptor);

            var fRet = new FileStreamResult(stream, MimeTypeMap.GetMimeType(Path.GetExtension(streamName)))
            {
                FileDownloadName = streamName
            };

            if (!(result.Result is Stream))
            {
                var json = result.Result.ToJson();
                _context.Response.Headers.Add(ClientConstValue.CustomResultHeaderKey, json);
            }

            var executor = new FileStreamResultExecutor(NullLoggerFactory.Instance);
            await executor.ExecuteAsync(actionContext, fRet);
        }

        public async Task SendAsync(Result result)
        {
            _context.Response.ContentType = "application/json; charset=utf-8";
            _context.Response.StatusCode = result.StatusCode;
            await _context.Response.WriteAsync(result.Ret.ToJson() ?? "");
        }
    }
}