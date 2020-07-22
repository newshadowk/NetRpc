using System;
using System.IO;
using System.IO.Pipelines;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NetRpc
{
    internal sealed class BufferClientOnceApiConvert : IClientOnceApiConvert
    {
        private readonly DuplexPipe _streamPipe = new DuplexPipe(new PipeOptions(pauseWriterThreshold: Helper.StreamBufferCacheCount, resumeWriterThreshold: 1));
        private readonly IClientConnection _connection;
        private readonly ILogger _logger;

        public event EventHandler<EventArgsT<object>>? ResultStream;
        public event EventHandler<EventArgsT<object>>? Result;
        public event AsyncEventHandler<EventArgsT<object>>? CallbackAsync;
        public event EventHandler<EventArgsT<object>>? Fault;

        public BufferClientOnceApiConvert(IClientConnection connection, ILogger logger)
        {
            _connection = connection;
            _logger = logger;
            _connection.ReceivedAsync += ConnectionReceivedAsync;
            _connection.ReceiveDisconnected += ConnectionReceiveDisconnected;
        }

        public ConnectionInfo ConnectionInfo => _connection.ConnectionInfo;

        public async Task StartAsync(string? authorizationToken)
        {
            await _connection.StartAsync(authorizationToken);
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
            return _connection.SendAsync(new Request(RequestType.BufferEnd).All, true);
        }

        public async Task<bool> SendCmdAsync(OnceCallParam callParam, MethodContext methodContext, Stream? stream, bool isPost, CancellationToken token)
        {
            await _connection.SendAsync(
                new Request(RequestType.Cmd, callParam.ToBytes()).All,
                stream == null && token == CancellationToken.None,
                isPost);
            return true;
        }

        public void Dispose()
        {
            _connection?.Dispose();
            _streamPipe.Dispose();
        }

#if NETSTANDARD2_1 || NETCOREAPP3_1
        public async ValueTask DisposeAsync()
        {
            if (_connection != null)
                await _connection.DisposeAsync();
            await _streamPipe.DisposeAsync();
        }
#endif

        private Stream GetReplyStream(long length)
        {
            var stream = new ProxyStream(_streamPipe.Input.AsStream(), length);

#pragma warning disable 1998
            async Task OnEnd(object sender, EventArgs e)
#pragma warning restore 1998
            {
                ((ReadStream) sender).FinishedAsync -= OnEnd;
#if NETSTANDARD2_1 || NETCOREAPP3_1
                await DisposeAsync();
#else
                Dispose();
#endif
            }

            stream.FinishedAsync += OnEnd;
            return stream;
        }

        private void ConnectionReceiveDisconnected(object sender, EventArgsT<Exception> e)
        {
            OnFault(new EventArgsT<object>(new ReceiveDisconnectedException(e.Value.Message)));
            Dispose();
        }

        private async Task ConnectionReceivedAsync(object sender, EventArgsT<ReadOnlyMemory<byte>> e)
        {
            var r = new Reply(e.Value);
            switch (r.Type)
            {
                case ReplyType.ResultStream:
                {
                    if (TryToObject(r.Body, out long length))
                        OnResultStream(new EventArgsT<object>(GetReplyStream(length)));
                    else
                        await OnFaultSerializationExceptionAsync();
                    break;
                }
                case ReplyType.CustomResult:
                {
                    if (TryToObject(r.Body, out CustomResult body))
                    {
                        if (body.HasStream)
                        {
                            var obj = body.Result.SetStream(GetReplyStream(body.StreamLength));
                            OnResultStream(new EventArgsT<object>(obj));
                        }
                        else
                        {
                            OnResult(new EventArgsT<object>(body.Result));
                            await InvokeDisposeAsync();
                        }
                    }
                    else
                        await OnFaultSerializationExceptionAsync();

                    break;
                }
                case ReplyType.Callback:
                {
                    if (TryToObject(r.Body, out var body))
                        await OnCallbackAsync(new EventArgsT<object>(body!));
                    else
                        await OnFaultSerializationExceptionAsync();
                    break;
                }
                case ReplyType.Fault:
                {
                    if (TryToObject(r.Body, out var body))
                    {
                        OnFault(new EventArgsT<object>(body!));
                        await InvokeDisposeAsync();
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
                    await InvokeDisposeAsync();
                    break;
                case ReplyType.BufferFault:
                    await _streamPipe.OutputStream.ComWriteAsync(r.Body);
                    await _streamPipe.Output.CompleteAsync(new BufferException());
                    await InvokeDisposeAsync();
                    break;
                case ReplyType.BufferEnd:
                    await _streamPipe.OutputStream.ComWriteAsync(r.Body);
                    await _streamPipe.Output.CompleteAsync();
                    await InvokeDisposeAsync();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void OnResult(EventArgsT<object> e)
        {
            Result?.Invoke(this, e);
        }

        private Task OnCallbackAsync(EventArgsT<object> e)
        {
            return CallbackAsync.InvokeAsync(this, e);
        }

        private void OnFault(EventArgsT<object> e)
        {
            Fault?.Invoke(this, e);
        }

        private void OnResultStream(EventArgsT<object> e)
        {
            ResultStream?.Invoke(this, e);
        }

        private bool TryToObject(ReadOnlyMemory<byte> body, out object? obj)
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
                obj = (T) obj2!;
                return true;
            }

            obj = default!;
            return false;
        }

        private async Task OnFaultSerializationExceptionAsync()
        {
            OnFault(new EventArgsT<object>(new SerializationException("Deserialization failure when receive data.")));
            await InvokeDisposeAsync();
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        private async Task InvokeDisposeAsync()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
#if NETSTANDARD2_1 || NETCOREAPP3_1
            await DisposeAsync();
#else
            Dispose();
#endif
        }
    }
}