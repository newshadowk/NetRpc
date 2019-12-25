using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NetRpc.Http.Client;

namespace NetRpc.Http
{
    internal sealed class HttpConnection : IDisposable
    {
        private readonly HttpContext _context;
        private readonly IHubContext<CallbackHub, ICallback> _hub;
        private readonly ILogger _logger;
        private readonly RateAction _ra = new RateAction(1000);
        private readonly ProgressEvent _progressEvent = new ProgressEvent();

        public HttpConnection(HttpContext context, IHubContext<CallbackHub, ICallback> hub, ILogger logger)
        {
            _context = context;
            _hub = hub;
            _logger = logger;
        }

        public string ConnectionId { get; set; }

        public string CallId { get; set; }

        public ProxyStream Stream
        {
            set
            {
                value.Progress += (s, e) =>
                {
                    ProgressEventArgs args;
                    if (value.Length == 0)
                        args = _progressEvent.DownLoaderProgress(e.Value, long.MaxValue);
                    else
                        args = _progressEvent.DownLoaderProgress(e.Value, value.Length);

                    _ra.Post(() =>
                    {
#pragma warning disable 4014
                        UploadProgress(args);
#pragma warning restore 4014
                    });
                };
            }
        }

        public async Task CallBack(object callbackObj)
        {
            try
            {
                if (ConnectionId != null)
                    await _hub.Clients.Client(ConnectionId).Callback(CallId, callbackObj.ToDtoJson());
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, null);
            }
        }

        private async Task UploadProgress(ProgressEventArgs args)
        {
            try
            {
                if (ConnectionId != null)
                    await _hub.Clients.Client(ConnectionId).UploadProgress(CallId, args.ToDtoJson());
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, null);
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
                var json = result.Result.ToDtoJson();
                json = HttpUtility.UrlEncode(json, Encoding.UTF8);
                _context.Response.Headers.Add(ClientConstValue.CustomResultHeaderKey, json);
            }

            var executor = new FileStreamResultExecutor(NullLoggerFactory.Instance);
            await executor.ExecuteAsync(actionContext, fRet);
        }

        public async Task SendAsync(Result result)
        {
            _context.Response.ContentType = "application/json; charset=utf-8";
            _context.Response.StatusCode = result.StatusCode;
            string s;
            if (result.IsPainText)
                s = result.Ret.ToString();
            else
                s = result.Ret.ToDtoJson();
            await _context.Response.WriteAsync(s ?? "");
        }

        public void Dispose()
        {
            _ra?.Dispose();
            _progressEvent?.Dispose();
        }
    }
}