using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Primitives;

namespace NetRpc.Http
{
    internal sealed class HttpNetRpcServiceOnceApiConvert : IHttpServiceOnceApiConvert
    {
        private readonly HttpContext _context;
        private readonly HttpConnection _connection;
        private readonly string _rootPath;
        private readonly bool _ignoreWhenNotMatched;
        private readonly object[] _instances;
        private CancellationTokenSource _cts;

        public HttpNetRpcServiceOnceApiConvert(HttpContext context, string rootPath, bool ignoreWhenNotMatched, IHubContext<CallbackHub, ICallback> hub, object[] instances)
        {
            _context = context;
            _connection = new HttpConnection(context, hub);
            _rootPath = rootPath;
            _ignoreWhenNotMatched = ignoreWhenNotMatched;
            _instances = instances;
            CallbackHub.Canceled += CallbackHubCanceled;
        }

        public void Start(CancellationTokenSource cts)
        {
            _cts = cts;
        }

        public Task<OnceCallParam> GetOnceCallParamAsync()
        {
            var actionInfo = GetActionInfo();
            var header = GetHeader();
            var args = GetArgs(actionInfo);
            var param = new OnceCallParam(header, actionInfo, null, args);

            if (header.TryGetValue("ConnectionId", out var connectionId))
                _connection.ConnectionId = connectionId.ToString();
            if (header.TryGetValue("CallId", out var callId))
                _connection.CallId = callId.ToString();
            return Task.FromResult(param);
        }

        public async Task SendResultAsync(CustomResult result, ActionInfo action, object[] args)
        {
            await _connection.SendAsync(HttpResult.FromOk(result.Result));
        }

        public async Task SendFaultAsync(Exception body, ActionInfo action, object[] args)
        {
            if (body is HttpNotMatchedException ||
                body is MethodNotFoundException)
            {
                NotMatched = true;
                if (_ignoreWhenNotMatched)
                    return;
            }

            await _connection.SendAsync(HttpResult.FromEx(body));
        }

        public async Task SendStreamAsync(Stream stream, string streamName)
        {
            await _connection.SendAsync(stream, streamName);
        }

        public Task SendCallbackAsync(object callbackObj, ActionInfo action, object[] args)
        {
            return _connection.CallBack(callbackObj);
        }

        public Stream GetRequestStream(long? length)
        {
            if (_context.Request.ContentType == null ||
                !_context.Request.ContentType.StartsWith("multipart/form-data"))
                return null;

            if (_context.Request.Form.Files.Count > 0)
            {
                IFormFile formFile = _context.Request.Form.Files[0];
                return formFile.OpenReadStream();
            }

            return null;
        }

        public void Dispose()
        {
            CallbackHub.Canceled -= CallbackHubCanceled;
        }

        public bool NotMatched { get; private set; }

        private ActionInfo GetActionInfo()
        {
            var rawPath = Helper.FormatPath(_context.Request.Path.Value);
            if (!string.IsNullOrEmpty(_rootPath))
            {
                var startS = $"{_rootPath}/".TrimStart('/');
                if (!rawPath.StartsWith(startS))
                    throw new HttpNotMatchedException($"Request url:'{_context.Request.Path.Value}' is start with '{startS}'");
                rawPath = rawPath.Substring(startS.Length);
            }

            return new ActionInfo
            {
                FullName = rawPath
            };
        }

        private Dictionary<string, object> GetHeader()
        {
            var ret = new Dictionary<string, object>();
            foreach (KeyValuePair<string, StringValues> pair in _context.Request.Headers)
                ret.Add(pair.Key, pair.Value);
            return ret;
        }

        private object[] GetArgs(ActionInfo ai)
        {
            //dataObjType
            (MethodInfo method, _) = ApiWrapper.GetMethodInfo(ai, _instances);
            var dataObjType = Helper.GetArgType(method);

            if (_context.Request.ContentType != null)
            {
                //multipart/form-data
                if (_context.Request.ContentType.StartsWith("multipart/form-data"))
                {
                    if (!_context.Request.Form.TryGetValue("data", out var data))
                        return new object[0];

                    if (data.Count == 0)
                        throw new HttpFailedException("multipart/form-data, 'data' have no data.");

                    var dataObj = Helper.ToObjectForHttp(data[0], dataObjType);
                    return Helper.GetArgsFromDataObj(dataObjType, dataObj);
                }

                //application/json
                if (_context.Request.ContentType == "application/json")
                {
                    string body;
                    using (var sr = new StreamReader(_context.Request.Body, Encoding.UTF8))
                        body = sr.ReadToEnd();

                    var dataObj = Helper.ToObjectForHttp(body, dataObjType);
                    return Helper.GetArgsFromDataObj(dataObjType, dataObj);
                }
            }
            else
            {
                var args = Helper.GetArgsFromQuery(_context.Request.Query, dataObjType);
                return args;
            }

            throw new HttpFailedException($"ContentType:'{_context.Request.ContentType}' is not supported.");
        }

        private void CallbackHubCanceled(object sender, EventArgs e)
        {
            _cts.Cancel();
        }
    }
}