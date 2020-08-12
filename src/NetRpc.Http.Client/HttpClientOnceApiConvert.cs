using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using NetRpc.Contract;
using RestSharp;

namespace NetRpc.Http.Client
{
    internal sealed class HttpClientOnceApiConvert : IClientOnceApiConvert
    {
        private readonly string _apiUrl;
        private readonly string _connId;
        private readonly HubCallBackNotifier _notifier;
        private readonly int _timeoutInterval;
        private TypeName? _callbackAction;
        private readonly string _callId = Guid.NewGuid().ToString();

        public event EventHandler<EventArgsT<object>>? ResultStream;
        public event AsyncEventHandler<EventArgsT<object?>>? ResultAsync;
        public event AsyncEventHandler<EventArgsT<object>>? CallbackAsync;
        public event AsyncEventHandler<EventArgsT<object>>? FaultAsync;

        public HttpClientOnceApiConvert(string apiUrl, string connectionId, HubCallBackNotifier notifier, int timeoutInterval)
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
                    ChannelType = ChannelType.Http
                };
            }
        }

        public Task StartAsync(string? authorizationToken)
        {
            return Task.CompletedTask;
        }

        public Task SendCancelAsync()
        {
            return _notifier.CancelAsync(_callId);
        }

        public Task SendBufferAsync(ReadOnlyMemory<byte> body)
        {
            return Task.CompletedTask;
        }

        public Task SendBufferEndAsync()
        {
            return Task.CompletedTask;
        }

        public async Task<bool> SendCmdAsync(OnceCallParam callParam, MethodContext methodContext, Stream? stream, bool isPost, CancellationToken token)
        {
            _callbackAction = methodContext.ContractMethod.Route.DefaultRout.MergeArgType.CallbackAction;
            var postObj = methodContext.ContractMethod.CreateMergeArgTypeObj(_callId, _connId, stream?.Length ?? 0, callParam.PureArgs);
            var actionPath = methodContext.ContractMethod.Route.DefaultRout.Path;
            var reqUrl = $"{_apiUrl}/{actionPath}";

            var client = new RestClient(reqUrl);
            client.Encoding = Encoding.UTF8;
            client.Timeout = _timeoutInterval;
            var req = new RestRequest(Method.POST);

            //header
            if (callParam.Header != null)
            {
                foreach (var pair in callParam.Header)
                    req.AddHeader(pair.Key, pair.Value?.ToString()!);
            }

            //request
            if (methodContext.ContractMethod.Route.DefaultRout.MergeArgType.StreamPropName != null)
            {
                req.AddParameter("data", postObj.ToDtoJson()!, ParameterType.RequestBody);
                // ReSharper disable once PossibleNullReferenceException
                req.AddFile(methodContext.ContractMethod.Route.DefaultRout.MergeArgType.StreamPropName!, stream!.CopyTo, 
                    methodContext.ContractMethod.Route.DefaultRout.MergeArgType.StreamPropName!,
                    stream!.Length);
            }
            else
            {
                req.AddJsonBody(postObj!);
            }

            //send request
            var realRetT = methodContext.ContractMethod.MethodInfo.ReturnType.GetTypeFromReturnTypeDefinition();

            //cancel
#pragma warning disable 4014
            // ReSharper disable once MethodSupportsCancellation
            Task.Run(async () =>
#pragma warning restore 4014
            {
                token.Register(async () => { await _notifier.CancelAsync(_callId); });

                if (token.IsCancellationRequested)
                    await _notifier.CancelAsync(_callId);
            });

            //ReSharper disable once MethodSupportsCancellation
            var res = await client.ExecuteAsync(req);

            //fault
            TryThrowFault(methodContext, res);

            //return stream
            if (realRetT.HasStream())
            {
                var ms = new MemoryStream(res.RawBytes);
                if (methodContext.ContractMethod.MethodInfo.ReturnType.GetTypeFromReturnTypeDefinition().IsStream())
                    OnResultStream(new EventArgsT<object>(ms));
                else
                {
                    var resultH = res.Headers.First(i => i.Name == ClientConstValue.CustomResultHeaderKey);
                    // ReSharper disable once PossibleNullReferenceException
                    var hStr = HttpUtility.UrlDecode(resultH.Value?.ToString(), Encoding.UTF8);
                    var retInstance = hStr.ToDtoObject(realRetT)!;
                    retInstance.SetStream(ms);
                    OnResultStream(new EventArgsT<object>(retInstance));
                }
            }
            //return object
            else
            {
                var value = res.Content.ToDtoObject(realRetT)!;
                await OnResultAsync(new EventArgsT<object?>(value));
            }

            //Dispose: all stream data already received.
            //When Exception occur before will dispose outside.
            await DisposeAsync();
            return false;
        }

        public ValueTask DisposeAsync()
        {
            if (_notifier != null)
                _notifier.Callback -= Notifier_Callback;
            return new ValueTask();
        }

        private static void TryThrowFault(MethodContext methodContext, IRestResponse res)
        {
            //OperationCanceledException
            if ((int) res.StatusCode == ClientConstValue.CancelStatusCode)
                throw new OperationCanceledException();

            //ResponseTextException
            var textAttrs = methodContext.ContractMethod.ResponseTextAttributes;
            var found2 = textAttrs.FirstOrDefault(i => i.StatusCode == (int) res.StatusCode);
            if (found2 != null)
                throw new ResponseTextException(res.Content, (int) res.StatusCode);

            //FaultException
            var attrs = methodContext.ContractMethod.FaultExceptionAttributes;
            foreach (var grouping in attrs.GroupBy(i => i.StatusCode))
            {
                if (grouping.Key == (int) res.StatusCode)
                {
                    var fObj = (FaultExceptionJsonObj) res.Content.ToDtoObject(typeof(FaultExceptionJsonObj))!;
                    var found = grouping.FirstOrDefault(i => i.ErrorCode == fObj.ErrorCode);
                    if (found != null)
                        throw CreateException(found.DetailType, res.Content);
                }
            }

            //DefaultException
            if ((int) res.StatusCode == ClientConstValue.DefaultExceptionStatusCode)
                throw CreateException(typeof(Exception), res.Content);
        }

        private Task OnResultAsync(EventArgsT<object?> e)
        {
            return ResultAsync.InvokeAsync(this, e);
        }

        private void OnResultStream(EventArgsT<object> e)
        {
            ResultStream?.Invoke(this, e);
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
                ex = (Exception) Activator.CreateInstance(exType, msg);
            }
            catch
            {
                ex = (Exception) Activator.CreateInstance(exType);
            }

            return Helper.WarpException(ex);
        }
    }
}