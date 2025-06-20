using Microsoft.AspNetCore.Http;
using NetRpc.Http.Client;

namespace NetRpc.Http;

internal sealed class HttpServiceOnceApiConvert : IServiceOnceApiConvert
{
    private readonly List<ContractInfo> _contracts;
    private readonly HttpContext _context;
    private readonly HttpConnection _connection;
    private readonly string? _rootPath;
    private readonly bool _ignoreWhenNotMatched;
    private readonly HttpObjProcessorManager _httpObjProcessorManager;

    public HttpServiceOnceApiConvert(List<ContractInfo> contracts,
        HttpContext context,
        string? rootPath,
        bool ignoreWhenNotMatched,
        HttpObjProcessorManager httpObjProcessorManager)
    {
        _contracts = contracts;
        _context = context;
        _connection = new HttpConnection(context);
        _rootPath = rootPath;
        _ignoreWhenNotMatched = ignoreWhenNotMatched;
        _httpObjProcessorManager = httpObjProcessorManager;
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

    public Task<bool> StartAsync(CancellationTokenSource cts)
    {
        return Task.FromResult(true);
    }

    public async Task<ServiceOnceCallParam> GetServiceOnceCallParamAsync()
    {
        var (actionInfo, hri, rawPath) = GetActionInfo();
        var header = GetHeader();
        var httpObj = await GetHttpDataObjAndStream(hri, rawPath);

        var pureArgs = GetPureArgsFromDataObj(httpObj.HttpDataObj.Type, httpObj.HttpDataObj.Value, hri);
        return new ServiceOnceCallParam(actionInfo, pureArgs, httpObj.HttpDataObj.StreamLength, httpObj.ProxyStream, header);
    }

    public async Task SendResultAsync(CustomResult result, Stream? stream, string? streamName, ActionExecutingContext context)
    {
        if (!result.HasStream)
            await _connection.SendAsync(new Result(result.Result));
        else
            await _connection.SendWithStreamAsync(context, result, stream!, streamName);
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
        if (body.GetExceptionFrom<OperationCanceledException>(true) != null)
            return _connection.SendAsync(Result.FromFaultException(new FaultExceptionJsonObj(), ClientConst.CancelStatusCode));

        // ResponseTextException
        var textEx = body.GetExceptionFrom<ResponseTextException>();
        if (textEx != null)
            return _connection.SendAsync(Result.FromPainText(textEx.Text, textEx.StatusCode));

        // customs Exception
        // ReSharper disable once UseNullPropagation
        if (context != null)
        {
            var t = context.ContractMethod.FaultExceptionAttributes.FirstOrDefault(i => body.GetExceptionFrom(i.DetailType) != null);
            if (t != null)
                return _connection.SendAsync(Result.FromFaultException(new FaultExceptionJsonObj(t.ErrorCode, t.Description ?? body.Message), t.StatusCode));
        }

        // default Exception
        return _connection.SendAsync(Result.FromFaultException(new FaultExceptionJsonObj(null, body.Message), ClientConst.DefaultExceptionStatusCode));
    }

    public Task SendCallbackAsync(object? callbackObj)
    {
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
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
        var rawPath = Helper.FormatPath(_context.Request.Path.Value!);
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
        return await _httpObjProcessorManager.ProcessAsync(new ProcessItem(_context.Request, hri, rawPath, hri.MergeArgType.Type, hri.MergeArgType.TypeWithoutPathQueryStream));
    }
}