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
        private readonly CancellationTokenSource _timeOutCts = new();
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

        public async Task<object?> CallAsync(Dictionary<string, object?> header, MethodContext methodContext, Func<object?, Task>? callback,
            CancellationToken token, Stream? stream, params object?[] pureArgs)
        {
            if (callback != null)
                _callbackDispatcher = new AsyncDispatcher();

            var action = methodContext.InstanceMethod.MethodInfo.ToActionInfo();
            var tcs = new TaskCompletionSource<object?>();
            var t = Task.Run(async () =>
            {
                _convert.ResultStream += (_, e) => { SetStreamResult(tcs, e.Value); };
                _convert.ResultAsync += async (_, e) => await SetResultAsync(tcs, e.Value);

                if (callback != null)
                    _convert.CallbackAsync += async (_, e) =>
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

                _convert.FaultAsync += async (_, e) => await SetFaultAsync(tcs, e.Value);

                try
                {
                    //Send cmd
                    var postStream = methodContext.ContractMethod.IsMQPost ? stream.StreamToBytes() : null;
                    var p = new OnceCallParam(header, action, postStream != null || stream != null,
                        postStream, stream.GetLength(), pureArgs);

                    if (token.IsCancellationRequested)
                    {
                        await SetCancelAsync(tcs);
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
                            await SetFaultAsync(tcs, e);
                        }
                    });

                    if (!sendStreamNext || stream == null)
                        return;

                    //timeout
#pragma warning disable 4014
                    Task.Delay(_timeoutInterval, _timeOutCts.Token).ContinueWith(async _ =>
#pragma warning restore 4014
                    {
                        await SetFaultAsync(tcs, new TimeoutException($"Service is not response over {_timeoutInterval} ms, time out."));
                    }, _timeOutCts.Token);

                    //send stream
                    await Helper.SendStreamAsync(_convert.SendBufferAsync, _convert.SendBufferEndAsync, stream, token, OnSendRequestStreamStarted);
                    OnSendRequestStreamFinished();
                }
                catch (Exception e)
                {
                    await SetFaultAsync(tcs, e);
                }
            }, token);

            if (t.IsCanceled)
                await SetCancelAsync(tcs);

            return await tcs.Task;
        }

        private void SetStreamResult(TaskCompletionSource<object?> tcs, object result)
        {
            //current thread is receive thread by lower layer (rabbitMQ or Grpc), can not be block.
            //run a thread to handle Stream result, avoid sync read stream by user.
            _callbackDispatcher?.Dispose();
            Task.Run(() => { tcs.SetResult(result); });
        }

        private async Task SetCancelAsync(TaskCompletionSource<object?> tcs)
        {
            // ReSharper disable once MethodHasAsyncOverload
            _reg?.Dispose();
            _timeOutCts.Cancel();
            _callbackDispatcher?.Dispose();
            await _convert.DisposeAsync();
            tcs.TrySetCanceled();
        }

        private async Task SetFaultAsync(TaskCompletionSource<object?> tcs, object result)
        {
            // ReSharper disable once MethodHasAsyncOverload
            _reg?.Dispose();
            _timeOutCts.Cancel();
            _callbackDispatcher?.Dispose();
            await _convert.DisposeAsync();
            tcs.TrySetException((Exception) result);
        }

        private async Task SetResultAsync(TaskCompletionSource<object?> tcs, object? result)
        {
            // ReSharper disable once MethodHasAsyncOverload
            _reg?.Dispose();
            _timeOutCts.Cancel();
            _callbackDispatcher?.Dispose();
            await _convert.DisposeAsync();
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