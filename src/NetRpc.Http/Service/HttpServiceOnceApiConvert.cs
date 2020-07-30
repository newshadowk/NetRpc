using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
        private readonly List<ContractInfo> _contracts;
        private readonly HttpContext _context;
        private readonly HttpConnection _connection;
        private readonly string? _rootPath;
        private readonly bool _ignoreWhenNotMatched;
        private readonly HttpObjProcessorManager _httpObjProcessorManager;
        private CancellationTokenSource? _cts;
        private readonly FormOptions _defaultFormOptions = new FormOptions();

        public HttpServiceOnceApiConvert(List<ContractInfo> contracts,
            HttpContext context,
            string? rootPath,
            bool ignoreWhenNotMatched,
            IHubContext<CallbackHub, ICallback> hub,
            HttpObjProcessorManager httpObjProcessorManager,
            IServiceProvider serviceProvider)
        {
            var logger = ((ILoggerFactory) serviceProvider.GetService(typeof(ILoggerFactory))).CreateLogger("NetRpc");
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
            var httpObj = await GetHttpDataObjAndStream(actionInfo, hri, rawPath);
            
            _connection.CallId = httpObj.HttpDataObj.CallId;
            _connection.ConnectionId = httpObj.HttpDataObj.ConnectionId;
            _connection.Stream = httpObj.ProxyStream;

            var pureArgs = Helper.GetPureArgsFromDataObj(httpObj.HttpDataObj.Type, httpObj.HttpDataObj.Value);
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

        public Task SendCallbackAsync(object? callbackObj)
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
            var ms = new MemoryStream();
            await section.Body.CopyToAsync(ms);
            var body = Encoding.UTF8.GetString(ms.ToArray());
            var dataObj = Helper.ToHttpDataObj(body, dataObjType);

            //stream
            section = await reader.ReadNextSectionAsync();
            ValidateSection(section);
            var fileName = GetFileName(section.ContentDisposition);
            if (fileName == null)
                throw new ArgumentNullException(null, "File name is null.");
            dataObj.TrySetStreamName(fileName);
            var proxyStream = new ProxyStream(section.Body, dataObj.StreamLength);
            return (dataObj, proxyStream);
        }

        private static string? Match(string src, string left, string right)
        {
            var r = Regex.Match(src, $"(?<={left}).+(?={right})");
            if (r.Captures.Count > 0)
                return r.Captures[0].Value;
            return null;
        }

        private static string? GetFileName(string contentDisposition)
        {
            //Content-Disposition: form-data; name="stream"; filename="t1.docx"
            return Match(contentDisposition, "filename=\"", "\"");
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

        private async Task<HttpObj> GetHttpDataObjAndStream(ActionInfo ai, HttpRoutInfo hri, string rawPath)
        {
            //dataObjType
            var contractMethod = ApiWrapper.GetContractMethod(ai, _contracts);
            var dataObjType = contractMethod.MergeArgType.Type;

            return await _httpObjProcessorManager.ProcessAsync(new ProcessItem(_context.Request, hri, rawPath, dataObjType));
        }

        private void CallbackHubCanceled(object? sender, string e)
        {
            if (_connection.CallId == e || string.IsNullOrEmpty(e))
                _cts?.Cancel();
        }
    }
}