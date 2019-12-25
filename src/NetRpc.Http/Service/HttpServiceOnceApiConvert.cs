using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
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
        private readonly FormOptions _defaultFormOptions = new FormOptions();

        public HttpServiceOnceApiConvert(List<Contract> contracts, HttpContext context, string rootPath, bool ignoreWhenNotMatched, IHubContext<CallbackHub, ICallback> hub,
            IServiceProvider serviceProvider)
        {
            var logger = ((ILoggerFactory)serviceProvider.GetService(typeof(ILoggerFactory))).CreateLogger("NetRpc");
            _contracts = contracts;
            _context = context;
            _connection = new HttpConnection(context, hub, logger);
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

        public async Task<ServiceOnceCallParam> GetServiceOnceCallParamAsync()
        {
            var actionInfo = GetActionInfo();
            var header = GetHeader();
            var (dataObj, stream) = await GetHttpDataObjAndStream(actionInfo);

            _connection.CallId = dataObj.CallId;
            _connection.ConnectionId = dataObj.ConnectionId;
            _connection.Stream = stream;

            var pureArgs = Helper.GetPureArgsFromDataObj(dataObj.Type, dataObj.Value);

            return new ServiceOnceCallParam(actionInfo, pureArgs, dataObj.StreamLength, stream, header);
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

        public async Task<(HttpDataObj obj, ProxyStream stream)> GetFromFormDataAsync(Type dataObjType)
        {
            var boundary = MultipartRequestHelper.GetBoundary(
                MediaTypeHeaderValue.Parse(_context.Request.ContentType),
                _defaultFormOptions.MultipartBoundaryLengthLimit);
            var reader = new MultipartReader(boundary, _context.Request.Body);
            var section = await reader.ReadNextSectionAsync();

            //body
            ValidateSection(section);
            MemoryStream ms = new MemoryStream();
            section.Body.CopyTo(ms);
            var body = Encoding.UTF8.GetString(ms.ToArray());
            var dataObj = Helper.ToHttpDataObj(body, dataObjType);

            //stream
            section = await reader.ReadNextSectionAsync();
            ValidateSection(section);
            var proxyStream = new ProxyStream(section.Body, dataObj.StreamLength);

            return (dataObj, proxyStream);
        }

        private static void ValidateSection(MultipartSection section)
        {
            var hasContentDispositionHeader =
                ContentDispositionHeaderValue.TryParse(
                    section.ContentDisposition, out _);

            if (!hasContentDispositionHeader)
                throw new HttpFailedException("Has not ContentDispositionHeader.");
        }

        public void Dispose()
        {
            CallbackHub.Canceled -= CallbackHubCanceled;
            _connection.Dispose();
        }

#if NETCOREAPP3_1
        public ValueTask DisposeAsync()
        {
            Dispose();
            return new ValueTask();
        }
#endif

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

        private async Task<(HttpDataObj dataObj, ProxyStream stream)> GetHttpDataObjAndStream(ActionInfo ai)
        {
            //dataObjType
            var method = ApiWrapper.GetMethodInfo(ai, _contracts, _serviceProvider);
            var dataObjType = method.contractMethod.MergeArgType.Type;

            if (_context.Request.ContentType != null)
            {
                //multipart/form-data
                if (_context.Request.ContentType.StartsWith("multipart/form-data"))
                {
                    return await GetFromFormDataAsync(dataObjType);
                }

                //application/json
                if (_context.Request.ContentType.StartsWith("application/json"))
                {
                    string body;
                    using (var sr = new StreamReader(_context.Request.Body, Encoding.UTF8))
                        body = await sr.ReadToEndAsync();

                    var dataObj = Helper.ToHttpDataObj(body, dataObjType);
                    return (dataObj, null);
                }
            }

            throw new HttpFailedException($"ContentType:'{_context.Request.ContentType}' is not supported.");
        }

        private void CallbackHubCanceled(object sender, string e)
        {
            if (_connection.CallId == e || string.IsNullOrEmpty(e))
                _cts.Cancel();
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