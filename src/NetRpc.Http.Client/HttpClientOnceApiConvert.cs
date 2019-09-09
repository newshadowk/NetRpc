using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RestSharp;

namespace NetRpc.Http.Client
{
    internal sealed class HttpClientOnceApiConvert : IClientOnceApiConvert
    {
        private readonly Type _contactType;
        private readonly string _apiUrl;
        private volatile string _connectionId;
        private readonly HubCallBackNotifier _notifier;
        private readonly int _timeoutInterval;
        private TypeName _callBackAction;
        private string _callId;

        public event EventHandler<EventArgsT<object>> ResultStream;
        public event EventHandler<EventArgsT<object>> Result;
        public event EventHandler<EventArgsT<object>> Callback;
        public event EventHandler<EventArgsT<object>> Fault;

        public HttpClientOnceApiConvert(Type contactType, string apiUrl, string connectionId, HubCallBackNotifier notifier, int timeoutInterval)
        {
            _contactType = contactType;
            _apiUrl = apiUrl;
            _connectionId = connectionId;
            _notifier = notifier;
            _timeoutInterval = timeoutInterval + 1000 * 5;
            if (_notifier != null)
                _notifier.Callback += Notifier_Callback;
        }

        private void Notifier_Callback(object sender, CallbackEventArgs e)
        {
            if (e.CallId != _callId)
                return;
            var argType = _callBackAction.Type.GenericTypeArguments[0];
            var obj = e.Data.ToObject(argType);
            OnCallback(new EventArgsT<object>(obj));
        }

        public Task StartAsync()
        {
            return Task.CompletedTask;
        }

        public Task SendCancelAsync()
        {
            return _notifier.CancelAsync(_callId);
        }

        public Task SendBufferAsync(byte[] body)
        {
            return Task.CompletedTask;
        }

        public Task SendBufferEndAsync()
        {
            return Task.CompletedTask;
        }

        public async Task<bool> SendCmdAsync(OnceCallParam callParam, MethodInfo methodInfo, Stream stream, bool isPost, CancellationToken token)
        {
            var postType = ClientHelper.GetArgType(methodInfo, true, out var streamName, out _callBackAction, out var outToken);

            var postObj = GetPostObj(postType, _callBackAction != null || outToken != null, callParam.Args);
            var actionPath = ClientHelper.GetActionPath(_contactType, methodInfo);
            var reqUrl = $"{_apiUrl}/{actionPath}";

            var client = new RestClient(reqUrl);
            client.Encoding = Encoding.UTF8;
            client.Timeout = _timeoutInterval;
            var req = new RestRequest(Method.POST);

            //request
            if (streamName != null)
            {
                req.AddParameter("data", postObj.ToJson(), ParameterType.RequestBody);
                req.AddFile(streamName, stream.CopyTo, streamName, stream.Length);
            }
            else
            {
                req.AddJsonBody(postObj);
            }

            //send request
            var realRetT = methodInfo.ReturnType.GetTypeFromReturnTypeDefinition();

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
            var res = await client.ExecuteTaskAsync(req);

            //fault
            TryThrowFault(methodInfo, res);

            //return stream
            if (realRetT.HasStream())
            {
                var ms = new MemoryStream(res.RawBytes);
                if (methodInfo.ReturnType.GetTypeFromReturnTypeDefinition() == typeof(Stream))
                    OnResultStream(new EventArgsT<object>(ms));
                else
                {
                    var resultH = res.Headers.First(i => i.Name == ClientConstValue.CustomResultHeaderKey);
                    var retInstance = resultH.Value.ToString().ToObject(realRetT);
                    retInstance.SetStream(ms);
                    OnResultStream(new EventArgsT<object>(retInstance));
                }
            }
            //return object
            else
            {
                var value = res.Content.ToObject(realRetT);
                OnResult(new EventArgsT<object>(value));
            }

            //Dispose: all stream data already received.
            Dispose();
            return false;
        }

        public void Dispose()
        {
            if (_notifier != null)
                _notifier.Callback -= Notifier_Callback;
        }

        private static void TryThrowFault(MethodInfo methodInfo, IRestResponse res)
        {
            if ((int) res.StatusCode == ClientConstValue.CancelStatusCode)
                throw new OperationCanceledException();

            var attrs = methodInfo.GetCustomAttributes<FaultExceptionAttribute>(true);
            var found = attrs.FirstOrDefault(i => i.StatusCode == (int) res.StatusCode);
            if (found != null)
                throw CreateException(found.DetailType, res.Content);

            var textAttrs = methodInfo.GetCustomAttributes<ResponseTextAttribute>(true);
            var found2 = textAttrs.FirstOrDefault(i => i.StatusCode == (int) res.StatusCode);
            if (found2 != null)
                throw new ResponseTextException(res.Content, (int) res.StatusCode);

            if ((int) res.StatusCode == ClientConstValue.DefaultExceptionStatusCode)
                throw CreateException(typeof(Exception), res.Content);
        }

        private object GetPostObj(Type postType, bool needSignalR, object[] args)
        {
            if (postType == null)
                return null;

            var instance = Activator.CreateInstance(postType);
            var newArgs = args.ToList();

            //_connectionId _callId
            if (needSignalR)
            {
                _callId = Guid.NewGuid().ToString();
                newArgs.Add(_connectionId);
                newArgs.Add(_callId);
            }

            var i = 0;
            foreach (var p in postType.GetProperties())
            {
                p.SetValue(instance, newArgs[i]);
                i++;
            }

            return instance;
        }

        private void OnResult(EventArgsT<object> e)
        {
            Result?.Invoke(this, e);
        }

        private void OnResultStream(EventArgsT<object> e)
        {
            ResultStream?.Invoke(this, e);
        }

        private void OnCallback(EventArgsT<object> e)
        {
            Callback?.Invoke(this, e);
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

        private void OnFault(EventArgsT<object> e)
        {
            Fault?.Invoke(this, e);
        }
    }
}