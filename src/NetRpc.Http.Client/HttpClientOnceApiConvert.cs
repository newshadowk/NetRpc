using System.Net.Http.Json;
using System.Text;
using System.Web;
using NetRpc.Contract;

namespace NetRpc.Http.Client;

internal sealed class HttpClientOnceApiConvert : IClientOnceApiConvert
{
    private readonly string _apiUrl;
    private readonly string _connId;
    private readonly HubCallBackNotifier? _notifier;
    private readonly int _timeoutInterval;
    private TypeName? _callbackAction;
    private readonly string _callId = Guid.NewGuid().ToString();

    public event EventHandler<EventArgsT<object>>? ResultStream;
    public event AsyncEventHandler<EventArgsT<object?>>? ResultAsync;
    public event AsyncEventHandler<EventArgsT<object>>? CallbackAsync;
    public event AsyncEventHandler<EventArgsT<object>>? FaultAsync;
    public event AsyncEventHandler? DisposingAsync;

    public HttpClientOnceApiConvert(string apiUrl, string connectionId, HubCallBackNotifier? notifier, int timeoutInterval)
    {
        _apiUrl = apiUrl;
        _connId = connectionId;
        _notifier = notifier;
        _timeoutInterval = timeoutInterval + 1000 * 5;
        if (_notifier != null)
            _notifier.Callback += Notifier_Callback;
    }

    private void Notifier_Callback(object? sender, CallbackEventArgs e)
    {
        if (e.CallId != _callId)
            return;

        var argType = _callbackAction!.Type.GenericTypeArguments[0];
        var obj = e.Data.ToDtoObject(argType)!;
        //have not deadlock issue?
        OnCallbackAsync(new EventArgsT<object>(obj)).Wait();
    }

    public ConnectionInfo ConnectionInfo
    {
        get
        {
            var host = new Uri(_apiUrl).Host;
            var port = new Uri(_apiUrl).Port;
            return new ConnectionInfo
            {
                Host = host,
                Port = port,
                Description = _apiUrl,
                HeadHost = host,
                ChannelType = ChannelType.Http
            };
        }
    }

    public Task StartAsync(Dictionary<string, object?> headers, bool isPost)
    {
        return Task.CompletedTask;
    }

    public Task SendCancelAsync()
    {
        return _notifier!.CancelAsync(_callId);
    }

    public Task SendBufferAsync(ReadOnlyMemory<byte> body)
    {
        return Task.CompletedTask;
    }

    public Task SendBufferEndAsync()
    {
        return Task.CompletedTask;
    }

    public async Task<bool> SendCmdAsync(OnceCallParam callParam, MethodContext methodContext, Stream? stream, bool isPost, byte mqPriority,
        CancellationToken token)
    {
        _callbackAction = methodContext.ContractMethod.Route.DefaultRout.MergeArgType.CallbackAction;
        var postObj = methodContext.ContractMethod.CreateMergeArgTypeObj(_callId, _connId, stream?.Length ?? 0, callParam.PureArgs);
        var actionPath = methodContext.ContractMethod.Route.DefaultRout.Path;
        var reqUrl = $"{_apiUrl}/{actionPath}";

        var client = new HttpClient();
        client.Timeout = TimeSpan.FromMilliseconds(_timeoutInterval);

        //header
        foreach (var pair in callParam.Header)
            client.DefaultRequestHeaders.Add(pair.Key, pair.Value?.ToString()!);

        HttpContent content;

        //request
        var streamPropName = methodContext.ContractMethod.Route.DefaultRout.MergeArgType.StreamPropName;
        if (streamPropName != null)
        {
            var mc = new MultipartFormDataContent("----WebKitFormBoundaryXQABsuJCc0RB2mEJ");
            //client.DefaultRequestHeaders.Add("Content-Type", "multipart/form-data");
            mc.Add(new StringContent(postObj.ToDtoJson()!), "data");
            mc.Add(new StreamContent(stream!), streamPropName, "testfilename");
            content = mc;
        }
        else
        {
            if (postObj == null)
                content = JsonContent.Create("");
            else
                content = JsonContent.Create(postObj);
        }

        //cancel
#pragma warning disable 4014
        // ReSharper disable once MethodSupportsCancellation
        Task.Run(async () =>
#pragma warning restore 4014
        {
            token.Register(() => { _notifier!.CancelAsync(_callId); });

            if (token.IsCancellationRequested)
                await _notifier!.CancelAsync(_callId);
        });

        //send request
        var res = await client.PostAsync(reqUrl, content, token);
        var contentStr = await res.Content.ReadAsStringAsync(token);

        //fault
        TryThrowFault(methodContext, contentStr, (int)res.StatusCode);

        //return stream
        var realRetT = methodContext.ContractMethod.MethodInfo.ReturnType.GetTypeFromReturnTypeDefinition();
        if (realRetT.HasStream())
        {
            var s = await res.Content.ReadAsStreamAsync(token);
            if (methodContext.ContractMethod.MethodInfo.ReturnType.GetTypeFromReturnTypeDefinition().IsStream())
                OnResultStream(new EventArgsT<object>(s));
            else
            {
                var resultH = res.Headers.First(i => i.Key == ClientConst.CustomResultHeaderKey);
                var hStr = HttpUtility.UrlDecode(resultH.Value.First(), Encoding.UTF8);
                var retInstance = hStr.ToDtoObject(realRetT)!;
                retInstance.SetStream(s);
                OnResultStream(new EventArgsT<object>(retInstance));
            }
        }
        //return object
        else
        {
            var value = contentStr.ToDtoObject(realRetT)!;
            await OnResultAsync(new EventArgsT<object?>(value));
        }

        //Dispose: all stream data already received.
        //When Exception occur before will dispose outside.
        await DisposeAsync();
        return false;
    }

    public async ValueTask DisposeAsync()
    {
        await OnDisposingAsync(EventArgs.Empty);
        if (_notifier != null)
            _notifier.Callback -= Notifier_Callback;
    }

    private static void TryThrowFault(MethodContext methodContext, string content, int statusCode)
    {
        //OperationCanceledException
        if (statusCode == ClientConst.CancelStatusCode)
            throw new OperationCanceledException();

        //ResponseTextException
        var textAttrs = methodContext.ContractMethod.ResponseTextAttributes;
        var found2 = textAttrs.FirstOrDefault(i => i.StatusCode == statusCode);
        if (found2 != null)
            throw new ResponseTextException(content, statusCode);

        //FaultException
        var attrs = methodContext.ContractMethod.FaultExceptionAttributes;
        foreach (var grouping in attrs.GroupBy(i => i.StatusCode))
        {
            if (grouping.Key == statusCode)
            {
                var fObj = (FaultExceptionJsonObj)content.ToDtoObject(typeof(FaultExceptionJsonObj))!;
                var found = grouping.FirstOrDefault(i => i.ErrorCode == fObj.ErrorCode);
                if (found != null)
                    throw CreateException(found.DetailType, content);
            }
        }

        //DefaultException
        if (statusCode == ClientConst.DefaultExceptionStatusCode)
            throw CreateException(typeof(Exception), content);
    }

    private Task OnResultAsync(EventArgsT<object?> e)
    {
        return ResultAsync.InvokeAsync(this, e);
    }

    private void OnResultStream(EventArgsT<object> e)
    {
        ResultStream?.Invoke(this, e);
    }

    private void OnFaultAsync(EventArgsT<object> e)
    {
        FaultAsync?.Invoke(this, e);
    }

    private Task OnDisposingAsync(EventArgs e)
    {
        return DisposingAsync.InvokeAsync(this, e);
    }

    private Task OnCallbackAsync(EventArgsT<object> e)
    {
        return CallbackAsync.InvokeAsync(this, e);
    }

    private static Exception CreateException(Type exType, string msg)
    {
        Exception ex;
        try
        {
            ex = (Exception)Activator.CreateInstance(exType, msg)!;
        }
        catch
        {
            ex = (Exception)Activator.CreateInstance(exType)!;
        }

        return Helper.WarpException(ex);
    }
}