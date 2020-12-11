using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using NetRpc.Contract;
using NetRpc.Http.Client;

namespace NetRpc.Http
{
    internal sealed class HttpServiceOnceApiConvert : IServiceOnceApiConvert
    {
        private readonly List<ContractInfo> _contracts;
        private readonly HttpContext _context;
        private readonly HttpConnection _connection;
        private readonly string? _rootPath;
        private readonly bool _ignoreWhenNotMatched;
        private readonly HttpObjProcessorManager _httpObjProcessorManager;
        private CancellationTokenSource? _cts;

        public HttpServiceOnceApiConvert(List<ContractInfo> contracts,
            HttpContext context,
            string? rootPath,
            bool ignoreWhenNotMatched,
            IHubContext<CallbackHub, ICallback> hub,
            HttpObjProcessorManager httpObjProcessorManager,
            ILoggerFactory loggerFactory)
        {
            var logger = loggerFactory.CreateLogger("NetRpc");
            _contracts = contracts;
            _context = context;
            _connection = new HttpConnection(context, hub, logger);
            _rootPath = rootPath;
            _ignoreWhenNotMatched = ignoreWhenNotMatched;
            _httpObjProcessorManager = httpObjProcessorManager;
            CallbackHub.Canceled += CallbackHubCanceled;
        }

        public Task SendBufferAsync(ReadOnlyMemory<byte> buffer)
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

        public async Task<ServiceOnceCallParam> GetServiceOnceCallParamAsync()
        {
            var (actionInfo, hri, rawPath) = GetActionInfo();
            var header = GetHeader();
            var httpObj = await GetHttpDataObjAndStream(hri, rawPath);

            _connection.CallId = httpObj.HttpDataObj.CallId;
            _connection.ConnId = httpObj.HttpDataObj.ConnId;
            _connection.Stream = httpObj.ProxyStream;

            var pureArgs = GetPureArgsFromDataObj(httpObj.HttpDataObj.Type, httpObj.HttpDataObj.Value, hri);
            return new ServiceOnceCallParam(actionInfo, pureArgs, httpObj.HttpDataObj.StreamLength, httpObj.ProxyStream, header);
        }

        public async Task<bool> SendResultAsync(CustomResult result, Stream? stream, string? streamName, ActionExecutingContext context)
        {
            if (!result.HasStream)
                await _connection.SendAsync(new Result(result.Result));
            else
                await _connection.SendWithStreamAsync(result, stream!, streamName!);
            return false;
        }

        public Task SendFaultAsync(Exception body, ActionExecutingContext? context)
        {
            if (body is HttpNotMatchedException ||
                body is MethodNotFoundException)
            {
                NotMatched = true;
                if (_ignoreWhenNotMatched)
                    return Task.CompletedTask;
            }

            // UnWarp FaultException
            body = NetRpc.Helper.UnWarpException(body);

            // Cancel
            if (body is OperationCanceledException)
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
                    return _connection.SendAsync(Result.FromFaultException(
                        new FaultExceptionJsonObj(t.ErrorCode, t.Description ?? body.Message), t.StatusCode));
            }

            // default Exception
            return _connection.SendAsync(Result.FromFaultException(new FaultExceptionJsonObj(null, body.Message), ClientConstValue.DefaultExceptionStatusCode));
        }

        public Task SendCallbackAsync(object? callbackObj)
        {
            return _connection.CallBack(callbackObj);
        }

        public ValueTask DisposeAsync()
        {
            CallbackHub.Canceled -= CallbackHubCanceled;
            _connection.Dispose();
            return new ValueTask();
        }

        public bool NotMatched { get; private set; }

        private static object?[] GetPureArgsFromDataObj(Type? dataObjType, object? dataObj, HttpRoutInfo hri)
        {
            var ret = new List<object?>();
            if (dataObjType == null)
                return ret.ToArray();

            if (hri.MergeArgType.IsSingleValue)
            {
                var sv = Activator.CreateInstance(hri.MergeArgType.SingleValue!.ParameterType)!;
                sv.CopyPropertiesFrom(dataObj!);
                ret.Add(sv);
                return ret.ToArray();
            }

            foreach (var p in dataObjType.GetProperties())
                ret.Add(NetRpc.Helper.GetPropertyValue(dataObj, p));
            return ret.ToArray();
        }

        private (ActionInfo ai, HttpRoutInfo hri, string rawPath) GetActionInfo()
        {
            var rawPath = Helper.FormatPath(_context.Request.Path.Value);
            if (!string.IsNullOrEmpty(_rootPath))
            {
                var startS = $"{_rootPath}/".TrimStart('/');
                if (!rawPath.StartsWith(startS))
                    throw new HttpNotMatchedException($"Request url:'{_context.Request.Path.Value}' is start with '{startS}'");
                rawPath = rawPath.Substring(startS.Length);
            }

            foreach (var contract in _contracts)
            foreach (var contractMethod in contract.Methods)
            {
                var hri = contractMethod.Route.MatchPath(rawPath, _context.Request.Method);
                if (hri != null)
                    return (contractMethod.MethodInfo.ToActionInfo(), hri, rawPath);
            }

            throw new HttpNotMatchedException($"Request url:'{_context.Request.Path.Value}' is not matched.");
        }

        private Dictionary<string, object?> GetHeader()
        {
            var ret = new Dictionary<string, object?>();
            foreach (var pair in _context.Request.Headers)
                ret.Add(pair.Key, pair.Value.Count > 0 ? pair.Value[0] : null);
            return ret;
        }

        private async Task<HttpObj> GetHttpDataObjAndStream(HttpRoutInfo hri, string rawPath)
        {
            //dataObjType
            return await _httpObjProcessorManager.ProcessAsync(new ProcessItem(_context.Request, hri, rawPath, hri.MergeArgType.Type));
        }

        private void CallbackHubCanceled(object? sender, string e)
        {
            if (_connection.CallId == e || string.IsNullOrEmpty(e))
                _cts?.Cancel();
        }
    }
}