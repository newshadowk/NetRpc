using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using NetRpc.Http.Client;

namespace NetRpc.Http
{
    internal sealed class HttpServiceOnceApiConvert : IServiceOnceApiConvert
    {
        private readonly List<Contract> _contracts;
        private readonly HttpContext _context;
        private readonly HttpConnection _connection;
        private readonly string _rootPath;
        private readonly bool _ignoreWhenNotMatched;
        private readonly IServiceProvider _serviceProvider;
        private CancellationTokenSource _cts;

        public HttpServiceOnceApiConvert(List<Contract> contracts, HttpContext context, string rootPath, bool ignoreWhenNotMatched, IHubContext<CallbackHub, ICallback> hub,
            IServiceProvider serviceProvider)
        {
            _contracts = contracts;
            _context = context;
            _connection = new HttpConnection(context, hub);
            _rootPath = rootPath;
            _ignoreWhenNotMatched = ignoreWhenNotMatched;
            _serviceProvider = serviceProvider;
            CallbackHub.Canceled += CallbackHubCanceled;
        }

        public Task SendBufferAsync(byte[] buffer)
        {
            return Task.CompletedTask;
        }

        public Task SendBufferEndAsync()
        {
            return Task.CompletedTask;
        }

        public Task SendBufferCancelAsync()
        {
            return Task.CompletedTask;
        }

        public Task SendBufferFaultAsync()
        {
            return Task.CompletedTask;
        }

        public Task StartAsync(CancellationTokenSource cts)
        {
            _cts = cts;
            return Task.CompletedTask;
        }

        public async Task<OnceCallParam> GetOnceCallParamAsync()
        {
            var actionInfo = GetActionInfo();
            var header = GetHeader();
            var pureArgs = await GetPureArgsAsync(actionInfo);
            var param = new OnceCallParam(header, actionInfo, null, null, pureArgs);
            return param;
        }

        public async Task<bool> SendResultAsync(CustomResult result, Stream stream, string streamName, ActionExecutingContext context)
        {
            if (!result.HasStream)
                await _connection.SendAsync(new Result(result.Result));
            else
                await _connection.SendWithStreamAsync(result, stream, streamName);

            return false;
        }

        public Task SendFaultAsync(Exception body, ActionExecutingContext context)
        {
            if (body is HttpNotMatchedException ||
                body is MethodNotFoundException)
            {
                NotMatched = true;
                if (_ignoreWhenNotMatched)
                    return Task.CompletedTask;
            }

            // Cancel
            if (body.GetType().IsEqualsOrSubclassOf(typeof(OperationCanceledException)))
                return _connection.SendAsync(Result.FromFaultException(new FaultExceptionJsonObj(), ClientConstValue.CancelStatusCode));

            // ResponseTextException
            if (body is ResponseTextException textEx)
                return _connection.SendAsync(Result.FromPainText(textEx.Text, textEx.StatusCode));

            // customs Exception
            // ReSharper disable once UseNullPropagation
            if (context != null)
            {
                var t = context.ContractMethod.FaultExceptionAttributes.FirstOrDefault(i => body.GetType() == i.DetailType);
                if (t != null)
                    return _connection.SendAsync(Result.FromFaultException(new FaultExceptionJsonObj(t.ErrorCode, body.Message), t.StatusCode));
            }

            // default Exception
            return _connection.SendAsync(Result.FromFaultException(new FaultExceptionJsonObj(0, body.Message), ClientConstValue.DefaultExceptionStatusCode));
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

            var actionInfo = GetActionInfo(rawPath);
            return actionInfo;
        }

        private Dictionary<string, object> GetHeader()
        {
            var ret = new Dictionary<string, object>();
            foreach (var pair in _context.Request.Headers)
                ret.Add(pair.Key, pair.Value.Count > 0 ? pair.Value[0] : null);
            return ret;
        }

        private async Task<object[]> GetPureArgsAsync(ActionInfo ai)
        {
            //dataObjType
            var method = ApiWrapper.GetMethodInfo(ai, _contracts, _serviceProvider);
            var dataObjType = method.contractMethod.MergeArgType.Type;

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
                    return Helper.GetPureArgsFromDataObj(dataObjType, dataObj);
                }

                //application/json
                if (_context.Request.ContentType.StartsWith("application/json"))
                {
                    string body;
                    using (var sr = new StreamReader(_context.Request.Body, Encoding.UTF8))
                        body = await sr.ReadToEndAsync();

                    var dataObj = Helper.ToObjectForHttp(body, dataObjType);
                    (_connection.ConnectionId, _connection.CallId) = GetConnectionIdCallId(dataObj);
                    return Helper.GetPureArgsFromDataObj(dataObjType, dataObj);
                }
            }
            else
            {
                return Helper.GetArgsFromQuery(_context.Request.Query, dataObjType);
            }

            throw new HttpFailedException($"ContentType:'{_context.Request.ContentType}' is not supported.");
        }

        private void CallbackHubCanceled(object sender, string e)
        {
            if (_connection.CallId == e || string.IsNullOrEmpty(e))
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
            if (dataObj == null)
                return (null, null);

            var connectionId = (string) GetValue(dataObj, ClientConstValue.ConnectionIdName);
            var callId = (string) GetValue(dataObj, ClientConstValue.CallIdName);
            return (connectionId, callId);
        }

        private ActionInfo GetActionInfo(string requestPath)
        {
            foreach (var contract in _contracts)
            foreach (var contractMethod in contract.ContractInfo.Methods)
            {
                if (requestPath == contractMethod.HttpRoutInfo.ToString())
                    return contractMethod.MethodInfo.ToActionInfo();
            }

            return null;
        }
    }
}