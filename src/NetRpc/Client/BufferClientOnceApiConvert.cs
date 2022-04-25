using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Pipelines;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NetRpc;

internal sealed class BufferClientOnceApiConvert : IClientOnceApiConvert
{
    private readonly IClientConnection _connection;
    private readonly ILogger _logger;
    private readonly DuplexPipe _streamPipe = new(new PipeOptions(pauseWriterThreshold: Helper.PipePauseWriterThreshold, resumeWriterThreshold: Helper.PipeResumeWriterThreshold));
    private int _disconnected;
    private volatile bool _streamResultReceived;
    private volatile bool _resultReceived;

    public BufferClientOnceApiConvert(IClientConnection connection, ILogger logger)
    {
        _connection = connection;
        _logger = logger;
        _connection.ReceivedAsync += ConnectionReceivedAsync;
        _connection.ReceiveDisconnected += ConnectionReceiveDisconnected;
    }

    public event EventHandler<EventArgsT<object>>? ResultStream;
    public event AsyncEventHandler<EventArgsT<object?>>? ResultAsync;
    public event AsyncEventHandler<EventArgsT<object>>? CallbackAsync;
    public event AsyncEventHandler<EventArgsT<object>>? FaultAsync;
    public event AsyncEventHandler? DisposingAsync;

    public ConnectionInfo ConnectionInfo => _connection.ConnectionInfo;

    public async Task StartAsync(Dictionary<string, object?> headers, bool isPost)
    {
        await _connection.StartAsync(headers, isPost);
    }

    public Task SendCancelAsync()
    {
        return _connection.SendAsync(new Request(RequestType.Cancel).All, true);
    }

    public Task SendBufferAsync(ReadOnlyMemory<byte> body)
    {
        return _connection.SendAsync(new Request(RequestType.Buffer, body).All);
    }

    public Task SendBufferEndAsync()
    {
        return _connection.SendAsync(new Request(RequestType.BufferEnd).All);
    }

    public async Task<bool> SendCmdAsync(OnceCallParam callParam, MethodContext methodContext, Stream? stream, bool isPost, byte mqPriority,
        CancellationToken token)
    {
        try
        {
            await _connection.SendAsync(new Request(RequestType.Cmd, callParam.ToBytes()).All, stream == null && token == CancellationToken.None, isPost,
                mqPriority);
        }
        catch (MqHandshakeException)
        {
            throw;
        }
        catch (MaxQueueCountException)
        {
            throw;
        }
        catch (Exception e)
        {
            _logger.LogWarning($"Client SendCmdAsync error. {callParam}");
            throw new DisconnectedException(e.Message);
        }

        return true;
    }

    public async ValueTask DisposeAsync()
    {
        await OnDisposingAsync(EventArgs.Empty);
        await _connection.DisposeAsync();
    }

    private Stream GetReplyStream(long length)
    {
        var stream = new ProxyStream(_streamPipe.Input.AsStream(), length, true);

        async Task OnEnd(object sender, EventArgs e)
        {
            ((ProxyStream)sender).FinishedAsync -= OnEnd;
            await DisposeAsync();
        }

        stream.FinishedAsync += OnEnd;
        return stream;
    }

    private async void ConnectionReceiveDisconnected(object? sender, EventArgsT<string> e)
    {
        if (Interlocked.Increment(ref _disconnected) == 1)
        {
            if (!_streamResultReceived)
            {
                await OnFaultAsync(new EventArgsT<object>(new DisconnectedException(e.Value)));
                await DisposeAsync();
                return;
            }

            await _streamPipe.Output.CompleteAsync(new DisconnectedException(e.Value));
            await DisposeAsync();
        }
    }

    private async Task<bool> CheckReplyRepeatAsync()
    {
        if (!_resultReceived)
            return false;

        if (_streamResultReceived)
        {
            await _streamPipe.Output.CompleteAsync(new ReplyRepeatException());
            await DisposeAsync();
            return true;
        }

        await OnFaultAsync(new EventArgsT<object>(new ReplyRepeatException()));
        await DisposeAsync();
        return true;
    }

    private async Task ConnectionReceivedAsync(object? sender, EventArgsT<ReadOnlyMemory<byte>> e)
    {
        var r = new Reply(e.Value);
        switch (r.Type)
        {
            case ReplyType.ResultStream:
            {
                if (await CheckReplyRepeatAsync())
                    return;

                _resultReceived = true;
                _streamResultReceived = true;
                if (TryToObject(r.Body, out long length))
                    OnResultStream(new EventArgsT<object>(GetReplyStream(length)));
                else
                    await OnFaultSerializationExceptionAsync();
                break;
            }
            case ReplyType.CustomResult:
            {
                if (await CheckReplyRepeatAsync())
                    return;

                _resultReceived = true;
                if (TryToObject(r.Body, out CustomResult body))
                {
                    if (body.HasStream)
                    {
                        _streamResultReceived = true;
                        var obj = body.Result.SetStream(GetReplyStream(body.StreamLength));
                        OnResultStream(new EventArgsT<object>(obj));
                    }
                    else
                    {
                        await OnResultAsync(new EventArgsT<object?>(body.Result));
                        await DisposeAsync();
                    }
                }
                else
                    await OnFaultSerializationExceptionAsync();

                break;
            }
            case ReplyType.Callback:
            {
                if (TryToObject(r.Body, out var body))
                    await OnCallbackAsync(new EventArgsT<object>(body));
                else
                    await OnFaultSerializationExceptionAsync();
                break;
            }
            case ReplyType.Fault:
            {
                if (TryToObject(r.Body, out var body))
                {
                    await OnFaultAsync(new EventArgsT<object>(body));
                    await DisposeAsync();
                }
                else
                    await OnFaultSerializationExceptionAsync();

                break;
            }
            case ReplyType.Buffer:
                await _streamPipe.OutputStream.ComWriteAsync(r.Body);
                break;
            case ReplyType.BufferCancel:
                await _streamPipe.OutputStream.ComWriteAsync(r.Body);
                await _streamPipe.Output.CompleteAsync(new TaskCanceledException());
                await DisposeAsync();
                break;
            case ReplyType.BufferFault:
                await _streamPipe.OutputStream.ComWriteAsync(r.Body);
                await _streamPipe.Output.CompleteAsync(new BufferException());
                await DisposeAsync();
                break;
            case ReplyType.BufferEnd:
                await _streamPipe.OutputStream.ComWriteAsync(r.Body);
                await _streamPipe.Output.CompleteAsync();
                await DisposeAsync();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private Task OnResultAsync(EventArgsT<object?> e)
    {
        return ResultAsync.InvokeAsync(this, e);
    }

    private Task OnCallbackAsync(EventArgsT<object> e)
    {
        return CallbackAsync.InvokeAsync(this, e);
    }

    private Task OnFaultAsync(EventArgsT<object> e)
    {
        return FaultAsync.InvokeAsync(this, e);
    }

    private Task OnDisposingAsync(EventArgs e)
    {
        return DisposingAsync.InvokeAsync(this, e);
    }

    private void OnResultStream(EventArgsT<object> e)
    {
        ResultStream?.Invoke(this, e);
    }

    private bool TryToObject(ReadOnlyMemory<byte> body, [NotNullWhen(true)] out object? obj)
    {
        try
        {
            obj = body.ToArray().ToObject<object>();
            return true;
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, null);
            obj = default;
            return false;
        }
    }

    private bool TryToObject<T>(ReadOnlyMemory<byte> body, out T obj)
    {
        if (TryToObject(body, out var obj2))
        {
            obj = (T)obj2!;
            return true;
        }

        obj = default!;
        return false;
    }

    private async Task OnFaultSerializationExceptionAsync()
    {
        await OnFaultAsync(new EventArgsT<object>(new SerializationException("Deserialization failure when receive data.")));
        await DisposeAsync();
    }
}