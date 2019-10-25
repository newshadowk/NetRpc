using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace NetRpc
{
    public sealed class OnceCall : IOnceCall
    {
        private readonly int _timeoutInterval;
        private readonly CancellationTokenSource _timeOutCts = new CancellationTokenSource();
        private readonly CancellationTokenSource _callbackCts = new CancellationTokenSource();
        private CancellationTokenRegistration? _reg;
        private readonly IClientOnceApiConvert _convert;
        private readonly BufferBlock<object> _callbackBB = new BufferBlock<object>();

        public OnceCall(IClientOnceApiConvert convert, int timeoutInterval)
        {
            _timeoutInterval = timeoutInterval;
            _convert = convert;
        }

        public async Task StartAsync()
        {
            await _convert.StartAsync();
        }

        public Task<object> CallAsync(Dictionary<string, object> header, MethodContext methodContext, Action<object> callback, CancellationToken token, Stream stream,
            params object[] pureArgs)
        {
            var action = methodContext.InstanceMethod.MethodInfo.ToActionInfo();

            var tcs = new TaskCompletionSource<object>();
            var t = Task.Run(async () =>
            {
                _convert.ResultStream += (s, e) => { SetStreamResult(tcs, e.Value); };
                _convert.Result += (s, e) => { SetResult(tcs, e.Value); };
                _convert.Callback += (s, e) => _callbackBB.Post(e.Value);
                _convert.Fault += (s, e) => { SetFault(tcs, e.Value); };

                //_callbackBB is make sure callback is the same context with call, scope which start with middleware.
#pragma warning disable 4014
                Task.Run(async () =>
#pragma warning restore 4014
                {
                    while (!_callbackCts.Token.IsCancellationRequested)
                        callback(await _callbackBB.ReceiveAsync(_callbackCts.Token));
                }, token);

                try
                {
                    //Send cmd
                    var postStream = methodContext.ContractMethod.IsMQPost ? stream.StreamToBytes() : null;
                    var p = new OnceCallParam(header, action, postStream, stream.GetLength(), pureArgs);
                    if (token.IsCancellationRequested)
                    {
                        SetCancel(tcs);
                        return;
                    }

                    var sendStreamNext = await _convert.SendCmdAsync(p, methodContext, stream, methodContext.ContractMethod.IsMQPost, token);
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

        private void SetStreamResult(TaskCompletionSource<object> tcs, object result)
        {
            //current thread is receive thread by lower layer (rabbitMq or Grpc), can not be block.
            //run a thread to handle Stream result, avoid sync read stream by user.
            _callbackCts.Cancel();
            Task.Run(() => { tcs.SetResult(result); });
        }

        private void SetCancel(TaskCompletionSource<object> tcs)
        {
            _reg?.Dispose();
            _timeOutCts.Cancel();
            _callbackCts.Cancel();
            _convert.Dispose();
            tcs.TrySetCanceled();
        }

        private void SetFault(TaskCompletionSource<object> tcs, object result)
        {
            _reg?.Dispose();
            _timeOutCts.Cancel();
            _callbackCts.Cancel();
            _convert.Dispose();
            tcs.TrySetException((Exception) result);
        }

        private void SetResult(TaskCompletionSource<object> tcs, object result)
        {
            _reg?.Dispose();
            _timeOutCts.Cancel();
            _callbackCts.Cancel();
            _convert.Dispose();
            tcs.TrySetResult(result);
        }
    }
}