using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NetRpc
{
    public sealed class OnceCall : IOnceCall
    {
        private readonly int _timeoutInterval;
        private readonly ILogger _logger;
        private readonly CancellationTokenSource _timeOutCts = new CancellationTokenSource();
        private AsyncDispatcher? _callbackDispatcher;
        private CancellationTokenRegistration? _reg;
        private readonly IClientOnceApiConvert _convert;

        public OnceCall(IClientOnceApiConvert convert, int timeoutInterval, ILogger logger)
        {
            _timeoutInterval = timeoutInterval;
            _logger = logger;
            _convert = convert;
        }

        public async Task StartAsync(string? authorizationToken)
        {
            await _convert.StartAsync(authorizationToken);
        }

        public event EventHandler? SendRequestStreamStarted;
        public event EventHandler? SendRequestStreamFinished;

        public ConnectionInfo ConnectionInfo => _convert.ConnectionInfo;

        public Task<object?> CallAsync(Dictionary<string, object?> header, MethodContext methodContext, Func<object?, Task>? callback, CancellationToken token,
            Stream? stream, params object?[] pureArgs)
        {
            if (callback != null)
                _callbackDispatcher = new AsyncDispatcher();

            var action = methodContext.InstanceMethod.MethodInfo.ToActionInfo();
            var tcs = new TaskCompletionSource<object?>();
            var t = Task.Run(async () =>
            {
                _convert.ResultStream += (s, e) => { SetStreamResult(tcs, e.Value); };
                _convert.Result += (s, e) => { SetResult(tcs, e.Value); };

                if (callback != null)
                    _convert.CallbackAsync += async (s, e) =>
                    {
                        try
                        {
                            await _callbackDispatcher!.InvokeAsync(() => callback(e.Value)).Unwrap();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "client callback");
                            throw;
                        }
                    };

                _convert.Fault += (s, e) => SetFault(tcs, e.Value);

                try
                {
                    //Send cmd
                    var postStream = methodContext.ContractMethod.IsMQPost ? stream.StreamToBytes() : null;
                    var p = new OnceCallParam(header, action, postStream != null || stream != null,
                        postStream, stream.GetLength(), pureArgs);

                    if (token.IsCancellationRequested)
                    {
                        SetCancel(tcs);
                        return;
                    }

                    //sendCmd
                    var sendStreamNext = await _convert.SendCmdAsync(p, methodContext, stream, methodContext.ContractMethod.IsMQPost, token);

                    //cancel token
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

                    if (!sendStreamNext || stream == null)
                        return;

                    //timeout
#pragma warning disable 4014
                    Task.Delay(_timeoutInterval, _timeOutCts.Token).ContinueWith(i =>
#pragma warning restore 4014
                    {
                        SetFault(tcs, new TimeoutException($"Service is not response over {_timeoutInterval} ms, time out."));
                    }, _timeOutCts.Token);

                    //send stream
                    await Helper.SendStreamAsync(_convert.SendBufferAsync, _convert.SendBufferEndAsync, stream, token, OnSendRequestStreamStarted);
                    OnSendRequestStreamFinished();
                }
                catch (Exception e)
                {
                    SetFault(tcs, e);
                }
            }, token);

            if (t.IsCanceled)
                SetCancel(tcs);

            return tcs.Task;
        }

        private void SetStreamResult(TaskCompletionSource<object?> tcs, object result)
        {
            //current thread is receive thread by lower layer (rabbitMQ or Grpc), can not be block.
            //run a thread to handle Stream result, avoid sync read stream by user.
            _callbackDispatcher?.Dispose();
            Task.Run(() => { tcs.SetResult(result); });
        }

        private void SetCancel(TaskCompletionSource<object?> tcs)
        {
            _reg?.Dispose();
            _timeOutCts.Cancel();
            _callbackDispatcher?.Dispose();
            _convert.Dispose();
            tcs.TrySetCanceled();
        }

        private void SetFault(TaskCompletionSource<object?> tcs, object result)
        {
            _reg?.Dispose();
            _timeOutCts.Cancel();
            _callbackDispatcher?.Dispose();
            _convert.Dispose();
            tcs.TrySetException((Exception) result);
        }

        private void SetResult(TaskCompletionSource<object?> tcs, object? result)
        {
            _reg?.Dispose();
            _timeOutCts.Cancel();
            _callbackDispatcher?.Dispose();
            _convert.Dispose();
            tcs.TrySetResult(result);
        }

        private void OnSendRequestStreamStarted()
        {
            SendRequestStreamStarted?.Invoke(this, EventArgs.Empty);
        }

        private void OnSendRequestStreamFinished()
        {
            SendRequestStreamFinished?.Invoke(this, EventArgs.Empty);
        }
    }
}