using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NetRpc;

public sealed class OnceCall : IOnceCall
{
    private readonly int _timeoutInterval;
    private readonly ILogger _logger;
    private readonly CancellationTokenSource _timeOutCts = new();
    private AsyncDispatcher? _callbackDispatcher;
    private CancellationTokenRegistration? _reg;
    private readonly IClientOnceApiConvert _convert;
    private OnceCallParam? _callParam;

    public OnceCall(IClientOnceApiConvert convert, int timeoutInterval, ILogger logger)
    {
        _timeoutInterval = timeoutInterval;
        _logger = logger;
        _convert = convert;
    }

    public async Task StartAsync(Dictionary<string, object?> headers, bool isPost)
    {
        await _convert.StartAsync(headers, isPost);
    }

    public event EventHandler? SendRequestStreamStarted;
    public event EventHandler? SendRequestStreamEndOrFault;

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
            _convert.FaultAsync += async (_, e) => await SetFaultAsync(tcs, e.Value);

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
                    }
                };

            try
            {
                // Send cmd
                var postStream = methodContext.ContractMethod.IsMQPost ? stream.StreamToBytes() : null;
                _callParam = new OnceCallParam(header, action, postStream != null || stream != null,
                    postStream, stream.GetLength(), pureArgs);

                if (token.IsCancellationRequested)
                {
                    await SetCancelAsync(tcs);
                    return;
                }

                // sendCmd
                var sendStreamNext = await _convert.SendCmdAsync(_callParam, methodContext, stream, methodContext.ContractMethod.IsMQPost, methodContext.ContractMethod.MqPriority, token);

                // cancel token
                // ReSharper disable once AsyncVoidLambda
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

                // timeout
#pragma warning disable 4014
                Task.Delay(TimeSpan.FromSeconds(60 * 20), _timeOutCts.Token).ContinueWith(async _ =>
#pragma warning restore 4014
                {
                    await SetFaultAsync(tcs, new TimeoutException($"Service is not response over {_timeoutInterval} ms, time out."));
                }, _timeOutCts.Token);

                // send stream
                await Helper.SendStreamAsync(_convert.SendBufferAsync, _convert.SendBufferEndAsync, stream, token, OnSendRequestStreamStarted, OnSendRequestStreamEndOrFault);
            }
            catch (Exception e)
            {
                await SetFaultAsync(tcs, e);
            }
        }, token);

        if (t.IsCanceled)
            await SetCancelAsync(tcs);

        var t32 = await tcs.Task;
        
        return t32;
    }

    private void SetStreamResult(TaskCompletionSource<object?> tcs, object result)
    {
        _callbackDispatcher?.Dispose();

        _convert.DisposingAsync += async (_, _) =>
        {
            if (_reg != null)
                await _reg.Value.DisposeAsync();
            _timeOutCts.Cancel();
        };

        //current thread is receive thread by lower layer (rabbitMQ or Grpc), can not be block.
        //run a thread to handle Stream result, avoid sync read stream by user.
        Task.Run(() => { tcs.SetResult(result); });
    }

    private async Task SetCancelAsync(TaskCompletionSource<object?> tcs)
    {
        if (_reg != null)
            await _reg.Value.DisposeAsync();
        _timeOutCts.Cancel();
        _callbackDispatcher?.Dispose();
        await _convert.DisposeAsync();
        tcs.TrySetCanceled();
    }

    private async Task SetFaultAsync(TaskCompletionSource<object?> tcs, object result)
    {
        if (result is SerializationException { Message: Const.DeserializationFailure } &&
            _callParam != null) 
            _logger.LogWarning($"SetFaultAsync, {_callParam}");

        if (_reg != null)
            await _reg.Value.DisposeAsync();
        _timeOutCts.Cancel();
        _callbackDispatcher?.Dispose();
        await _convert.DisposeAsync();

        //current thread is receive thread by lower layer (rabbitMQ or Grpc), can not be block.
#pragma warning disable CS4014
        Task.Run(() => { tcs.TrySetException((Exception)result); });
#pragma warning restore CS4014
    }

    private async Task SetResultAsync(TaskCompletionSource<object?> tcs, object? result)
    {
        if (_reg != null)
            await _reg.Value.DisposeAsync();
        _timeOutCts.Cancel();
        _callbackDispatcher?.Dispose();
        await _convert.DisposeAsync();

        //current thread is receive thread by lower layer (rabbitMQ or Grpc), can not be block.
#pragma warning disable CS4014
        Task.Run(() => { tcs.TrySetResult(result); });
#pragma warning restore CS4014
    }

    private void OnSendRequestStreamStarted()
    {
        SendRequestStreamStarted?.Invoke(this, EventArgs.Empty);
    }

    private void OnSendRequestStreamEndOrFault()
    {
        SendRequestStreamEndOrFault?.Invoke(this, EventArgs.Empty);
    }
}