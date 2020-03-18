using System;
using System.IO;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Logging;

namespace NetRpc
{
    internal sealed class BufferClientOnceApiConvert : IClientOnceApiConvert
    {
        private readonly IClientConnection _connection;
        private readonly ILogger _logger;

        private readonly BufferBlock<(byte[], BufferType)> _block =
            new BufferBlock<(byte[], BufferType)>(new DataflowBlockOptions {BoundedCapacity = Helper.StreamBufferCount});

        private BufferBlockStream _stream;

        public event EventHandler<EventArgsT<object>> ResultStream;
        public event EventHandler<EventArgsT<object>> Result;
        public event Func<object, EventArgsT<object>, Task> CallbackAsync;
        public event EventHandler<EventArgsT<object>> Fault;

        public BufferClientOnceApiConvert(IClientConnection connection, ILogger logger)
        {
            _connection = connection;
            _logger = logger;
            _connection.ReceivedAsync += ConnectionReceivedAsync;
            _connection.ReceiveDisconnected += ConnectionReceiveDisconnected;
        }

        public ConnectionInfo ConnectionInfo => _connection.ConnectionInfo;

        public async Task StartAsync()
        {
            await _connection.StartAsync();
        }

        public Task SendCancelAsync()
        {
            return _connection.SendAsync(new Request(RequestType.Cancel).All, true);
        }

        public Task SendBufferAsync(byte[] body)
        {
            return _connection.SendAsync(new Request(RequestType.Buffer, body).All);
        }

        public Task SendBufferEndAsync()
        {
            return _connection.SendAsync(new Request(RequestType.BufferEnd).All, true);
        }

        public async Task<bool> SendCmdAsync(OnceCallParam callParam, MethodContext methodContext, Stream stream, bool isPost, CancellationToken token)
        {
            await _connection.SendAsync(
                new Request(RequestType.Cmd, callParam.ToBytes()).All,
                stream == null && token == CancellationToken.None,
                isPost);
            return true;
        }

        private BufferBlockStream GetRequestStream(long length)
        {
            _stream = new BufferBlockStream(_block, length);

            void OnEnd(object sender, EventArgs e)
            {
                ((ReadStream) sender).Finished -= OnEnd;
                Dispose();
            }

            _stream.Finished += OnEnd;
            return _stream;
        }

        private void ConnectionReceiveDisconnected(object sender, EventArgsT<Exception> e)
        {
            OnFault(new EventArgsT<object>(new ReceiveDisconnectedException(e.Value.Message)));
            Dispose();
        }

        private async Task ConnectionReceivedAsync(object sender, EventArgsT<byte[]> e)
        {
            var r = new Reply(e.Value);
            switch (r.Type)
            {
                case ReplyType.ResultStream:
                {
                    if (TryToObject(r.Body, out long body))
                        OnResultStream(new EventArgsT<object>(GetRequestStream(body)));
                    else
                        OnFaultSerializationException();
                    break;
                }
                case ReplyType.CustomResult:
                {
                    if (TryToObject(r.Body, out CustomResult body))
                    {
                        if (body.HasStream)
                        {
                            var obj = body.Result.SetStream(GetRequestStream(body.StreamLength));
                            OnResultStream(new EventArgsT<object>(obj));
                        }
                        else
                        {
                            OnResult(new EventArgsT<object>(body.Result));
                            Dispose();
                        }
                    }
                    else
                        OnFaultSerializationException();
                    break;
                }
                case ReplyType.Callback:
                {
                    if (TryToObject(r.Body, out var body))
                        await OnCallbackAsync(new EventArgsT<object>(body));
                    else
                        OnFaultSerializationException();
                    break;
                }
                case ReplyType.Fault:
                {
                    if (TryToObject(r.Body, out var body))
                    {
                        OnFault(new EventArgsT<object>(body));
                        Dispose();
                    }
                    else
                        OnFaultSerializationException();
                    break;
                }
                case ReplyType.Buffer:
                    await _block.SendAsync((r.Body, BufferType.Buffer));
                    break;
                case ReplyType.BufferCancel:
                    await _block.SendAsync((default, BufferType.Cancel));
                    Dispose();
                    break;
                case ReplyType.BufferFault:
                    await _block.SendAsync((default, BufferType.Fault));
                    Dispose();
                    break;
                case ReplyType.BufferEnd:
                    await _block.SendAsync((r.Body, BufferType.End));
                    Dispose();
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

        private bool TryToObject(byte[] body, out object obj)
        {
            try
            {
                obj = body.ToObject<object>();
                return true;
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, null);
                obj = default;
                return false;
            }
        }

        private bool TryToObject<T>(byte[] body, out T obj)
        {
            if (TryToObject(body, out var obj2))
            {
                obj = (T) obj2;
                return true;
            }

            obj = default;
            return false;
        }

        private void OnFaultSerializationException()
        {
            OnFault(new EventArgsT<object>(new SerializationException("Deserialization failure when receive data.")));
            Dispose();
        }

        public void Dispose()
        {
            _connection?.Dispose();
        }

#if NETSTANDARD2_1 || NETCOREAPP3_1
        public ValueTask DisposeAsync()
        {
            if (_connection != null)
                return _connection.DisposeAsync();
            return new ValueTask();
        }
#endif
    }
}