using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace NetRpc
{
    public sealed class OnceCall<T> : IOnceCall<T>
    {
        private readonly int _timeoutInterval;
        private readonly string _traceId;
        private readonly CancellationTokenSource _timeOutCts = new CancellationTokenSource();
        private CancellationTokenRegistration? _reg;
        private readonly IClientOnceApiConvert _convert;

        public OnceCall(IClientOnceApiConvert convert, int timeoutInterval, string traceId)
        {
            _timeoutInterval = timeoutInterval;
            _traceId = traceId;
            _convert = convert;
        }

        public async Task StartAsync()
        {
            await _convert.StartAsync();
        }

        public Task<T> CallAsync(Dictionary<string, object> header, MethodInfo methodInfo, Action<object> callback, CancellationToken token, Stream stream,
            params object[] args)
        {
            var action = methodInfo.ToActionInfo();

            var tcs = new TaskCompletionSource<T>();
            var t = Task.Run(async () =>
            {
                _convert.ResultStream += (s, e) => { SetStreamResult(tcs, e.Value); };
                _convert.Result += (s, e) => { SetResult(tcs, e.Value); };
                _convert.Callback += (s, e) => { callback.Invoke(e.Value); };
                _convert.Fault += (s, e) => { SetFault(tcs, e.Value); };

                try
                {
                    //Send cmd
                    var postStream = action.IsPost ? stream.StreamToBytes() : null;
                    var p = new OnceCallParam(_traceId, header, action, postStream, stream.GetLength(), args);
                    if (token.IsCancellationRequested)
                    {
                        SetCancel(tcs);
                        return;
                    }

                    var sendStreamNext = await _convert.SendCmdAsync(p, methodInfo, stream, p.Action.IsPost, token);
                    if (!sendStreamNext)
                        return;

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

                    if (token.IsCancellationRequested)
                    {
                        SetCancel(tcs);
                        return;
                    }

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
            _convert.Dispose();
            tcs.TrySetCanceled();
        }

        private void SetFault(TaskCompletionSource<T> tcs, object result)
        {
            _reg?.Dispose();
            _timeOutCts.Cancel();
            _convert.Dispose();
            tcs.TrySetException((Exception) result);
        }

        private void SetResult(TaskCompletionSource<T> tcs, object result)
        {
            _reg?.Dispose();
            _timeOutCts.Cancel();
            _convert.Dispose();
            tcs.TrySetResult((T) result);
        }
    }
}