using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NetRpc
{
    internal sealed class OnceCall<T>
    {
        private readonly int _timeoutInterval;
        private readonly CancellationTokenSource _timeOutCts = new CancellationTokenSource();
        private CancellationTokenRegistration? _reg;
        private readonly ClientApiConvert _convert;
        private readonly IConnection _connection;
        private readonly bool _isWrapFaultException;

        public OnceCall(IConnection connection, bool isWrapFaultException, int timeoutInterval)
        {
            _connection = connection;
            _isWrapFaultException = isWrapFaultException;
            _timeoutInterval = timeoutInterval;
            _convert = new ClientApiConvert(connection);
        }

        public void Start()
        {
            _convert.Start();
        }

        public Task<T> CallAsync(Dictionary<string, object> header, ActionInfo action, Action<object> callback, CancellationToken token, Stream stream, 
            params object[] args)
        {
            var tcs = new TaskCompletionSource<T>();
            var t = Task.Run(async () =>
            {
                BufferBlockStream reciStream = _convert.GetRequestStream();

                _convert.End += (s, e) =>
                {
                    _connection.Dispose();
                };

                _convert.ResultStream += (s, e) =>
                {
                    SetStreamResult(tcs, reciStream);
                };

                _convert.Result += (s, e) =>
                {
                    if (e.HasStream)
                    {
                        var resultWithStream = e.Body.SetStream(reciStream);
                        SetStreamResult(tcs, resultWithStream);
                        return;
                    }

                    SetResult(tcs, e.Body);
                };

                _convert.Callback += (s, e) =>
                {
                    callback.Invoke(e.Value);
                };

                _convert.Fault += (s, e) =>
                {
                    SetFault(tcs, e.Value);
                };

                try
                {
                    //Send cmd
                    OnceCallParam p = new OnceCallParam(header, action, args);
                    if (token.IsCancellationRequested)
                    {
                        SetCancel(tcs);
                        return;
                    }

                    await _convert.SendCmdAsync(p);
                    _reg = token.Register(async () =>
                    {
                        try
                        {
                            await _convert.SendCancelAsync();
                        }
                        catch (Exception e)
                        {
                            SetFault(tcs, e);
                        }
                    });

                    //timeout
#pragma warning disable 4014
                    Task.Delay(_timeoutInterval, _timeOutCts.Token).ContinueWith(i =>
#pragma warning restore 4014
                    {
                        SetFault(tcs, new TimeoutException($"Service is not response over {_timeoutInterval} ms, time out."));
                    }, _timeOutCts.Token);

                    //Continue send stream
                    if (stream != null)
                        await Helper.SendStreamAsync(_convert.SendBufferAsync, _convert.SendBufferEndAsync, stream, token);
                }
                catch (Exception e)
                {
                    SetFault(tcs, e);
                }
            }, token);

            if (t.IsCanceled)
                tcs.TrySetCanceled();

            return tcs.Task;
        }

        private static void SetStreamResult(TaskCompletionSource<T> tcs, object result)
        {
            //current thread is receive thread by lower layer (rabbitMq or Grpc), can not be block.
            //run a thread to handle Stream result, avoid sync read stream by user.
            Task.Run(() => { tcs.SetResult((T) result); });
        }

        private void SetCancel(TaskCompletionSource<T> tcs)
        {
            _reg?.Dispose();
            _timeOutCts.Cancel();
            tcs.TrySetCanceled();
        }

        private void SetFault(TaskCompletionSource<T> tcs, object result)
        {
            if (!_isWrapFaultException && result is FaultException fe)
                result = fe.Detail;

            _reg?.Dispose();
            _timeOutCts.Cancel();
            tcs.TrySetException((Exception)result);
        }

        private void SetResult(TaskCompletionSource<T> tcs, object result)
        {
            _reg?.Dispose();
            _timeOutCts.Cancel();
            tcs.TrySetResult((T)result);
        }
    }
}