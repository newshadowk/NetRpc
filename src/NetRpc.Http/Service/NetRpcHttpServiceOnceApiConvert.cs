using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;

namespace NetRpc.Http
{
    internal sealed class NetRpcHttpServiceOnceApiConvert : IHttpServiceOnceApiConvert
    {
        private readonly List<Type> _contractTypes;
        private readonly HttpContext _context;
        private readonly HttpConnection _connection;
        private readonly string _rootPath;
        private readonly bool _ignoreWhenNotMatched;
        private CancellationTokenSource _cts;

        public NetRpcHttpServiceOnceApiConvert(List<Type> contractTypes, HttpContext context, string rootPath, bool ignoreWhenNotMatched, 
            IHubContext<CallbackHub, ICallback> hub, bool isClearStackTrace)
        {
            _contractTypes = contractTypes;
            _context = context;
            _connection = new HttpConnection(context, hub, isClearStackTrace);
            _rootPath = rootPath;
            _ignoreWhenNotMatched = ignoreWhenNotMatched;
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
            var param = new OnceCallParam(header, actionInfo, null, null, args);
            return Task.FromResult(param);
        }

        public async Task SendResultAsync(CustomResult result, RpcContext context)
        {
            await _connection.SendAsync(new Result(result.Result));
        }

        public Task SendFaultAsync(Exception body, RpcContext context)
        {
            if (body is HttpNotMatchedException ||
                body is MethodNotFoundException)
            {
                NotMatched = true;
                if (_ignoreWhenNotMatched)
                    return Task.CompletedTask;
            }

            //Cancel
            if (body.GetType().IsEqualsOrSubclassOf(typeof(OperationCanceledException)))
                return _connection.SendAsync(new Result(body, ConstValue.CancelStatusCode));

            //ResponseTextException
            if (body is ResponseTextException textEx)
                return _connection.SendAsync(new Result(textEx.Text, textEx.StatusCode));

            //customs Exception
            var t = context.ContractMethodInfo.GetCustomAttributes<NetRpcProducesResponseTypeAttribute>(true).FirstOrDefault(i => body.GetType() == i.DetailType);
            if (t != null)
                return _connection.SendAsync(new Result(body, t.StatusCode));

            //default Exception
            return _connection.SendAsync(new Result(body, ConstValue.DefaultExceptionStatusCode));
        }

        public async Task SendStreamAsync(Stream stream, string streamName)
        {
            await _connection.SendAsync(stream, streamName);
        }

        public Task SendCallbackAsync(object callbackObj)
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
                var formFile = _context.Request.Form.Files[0];
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
            foreach (var pair in _context.Request.Headers)
                ret.Add(pair.Key, pair.Value);
            return ret;
        }

        private object[] GetArgs(ActionInfo ai)
        {
            //dataObjType
            var method = ApiWrapper.GetMethodInfo(ai, _contractTypes.ToArray());
            var dataObjType = Helper.GetArgType(method.contractMethodInfo, out _, out _, out _);

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
                    (_connection.ConnectionId, _connection.CallId) = GetConnectionIdCallId(dataObj);
                    return Helper.GetArgsFromDataObj(dataObjType, dataObj);
                }

                //application/json
                if (_context.Request.ContentType == "application/json")
                {
                    string body;
                    using (var sr = new StreamReader(_context.Request.Body, Encoding.UTF8))
                        body = sr.ReadToEnd();

                    var dataObj = Helper.ToObjectForHttp(body, dataObjType);
                    (_connection.ConnectionId, _connection.CallId) = GetConnectionIdCallId(dataObj);
                    return Helper.GetArgsFromDataObj(dataObjType, dataObj);
                }
            }
            else
            {
                return Helper.GetArgsFromQuery(_context.Request.Query, dataObjType);
            }

            throw new HttpFailedException($"ContentType:'{_context.Request.ContentType}' is not supported.");
        }

        private void CallbackHubCanceled(object sender, EventArgs e)
        {
            _cts.Cancel();
        }

        private static object GetValue(object obj, string propertyName)
        {
            var pi = obj.GetType().GetProperty(propertyName);
            if (pi == null)
                return null;
            return pi.GetValue(obj);
        }

        private static (string connectionId, string callId) GetConnectionIdCallId(object dataObj)
        {
            var connectionId = (string) GetValue(dataObj, ConstValue.ConnectionIdName);
            var callId = (string) GetValue(dataObj, ConstValue.CallIdName);
            return (connectionId, callId);
        }
    }
}