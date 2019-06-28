using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace NetRpc
{
    internal sealed class ClientOnceApiConvert
    {
        private readonly IConnection _connection;

        private readonly BufferBlock<(byte[], BufferType)> _block =
            new BufferBlock<(byte[], BufferType)>(new DataflowBlockOptions {BoundedCapacity = Helper.StreamBufferCount});

        public event EventHandler<ResultStreamEventArgs> ResultStream;
        public event EventHandler End;
        public event EventHandler<EventArgsT<CustomResult>> CustomResult;
        public event EventHandler<EventArgsT<object>> Callback;
        public event EventHandler<EventArgsT<object>> Fault;

        public ClientOnceApiConvert(IConnection connection)
        {
            _connection = connection;
            _connection.Received += ConnectionReceived;
        }

        public void Start()
        {
            _connection.Start();
        }

        private void ConnectionReceived(object sender, EventArgsT<byte[]> e)
        {
            var r = new Reply(e.Value);
            switch (r.Type)
            {
                case ReplyType.ResultStream:
                {
                    if (TryToObject(r.Body, out long? body))
                        OnResultStream(new ResultStreamEventArgs(body));
                    else
                        OnFaultSerializationException();
                    break;
                }

                case ReplyType.CustomResult:
                {
                    if (TryToObject(r.Body, out CustomResult body))
                    {
                        OnCustomResult(new EventArgsT<CustomResult>(body));
                        if (!body.HasStream)
                            OnEnd();
                    }
                    else
                        OnFaultSerializationException();

                    break;
                }

                case ReplyType.Callback:
                {
                    if (TryToObject(r.Body, out object body))
                        OnCallback(new EventArgsT<object>(body));
                    else
                        OnFaultSerializationException();
                    break;
                }

                case ReplyType.Fault:
                {
                    if (TryToObject(r.Body, out object body))
                    {
                        OnFault(new EventArgsT<object>(body));
                        OnEnd();
                    }
                    else
                        OnFaultSerializationException();

                    break;
                }

                case ReplyType.Buffer:
                    _block.SendAsync((r.Body, BufferType.Buffer)).Wait();
                    break;
                case ReplyType.BufferCancel:
                    _block.SendAsync((default, BufferType.Cancel)).Wait();
                    OnEnd();
                    break;
                case ReplyType.BufferFault:
                    _block.SendAsync((default, BufferType.Fault)).Wait();
                    OnEnd();
                    break;
                case ReplyType.BufferEnd:
                    _block.SendAsync((r.Body, BufferType.End)).Wait();
                    OnEnd();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public Task SendCancelAsync()
        {
            return _connection.SendAsync(new Request(RequestType.Cancel).All);
        }

        public Task SendBufferAsync(byte[] body)
        {
            return _connection.SendAsync(new Request(RequestType.Buffer, body).All);
        }

        public Task SendBufferEndAsync()
        {
            return _connection.SendAsync(new Request(RequestType.BufferEnd).All);
        }

        public Task SendCmdAsync(OnceCallParam body)
        {
            return _connection.SendAsync(new Request(RequestType.Cmd, body.ToBytes()).All);
        }

        public BufferBlockStream GetRequestStream(long? length)
        {
            return new BufferBlockStream(_block, length);
        }

        private void OnCustomResult(EventArgsT<CustomResult> e)
        {
            CustomResult?.Invoke(this, e);
        }

        private void OnCallback(EventArgsT<object> e)
        {
            Callback?.Invoke(this, e);
        }

        private void OnFault(EventArgsT<object> e)
        {
            Fault?.Invoke(this, e);
        }

        private void OnResultStream(ResultStreamEventArgs e)
        {
            ResultStream?.Invoke(this, e);
        }

        private void OnEnd()
        {
            End?.Invoke(this, EventArgs.Empty);
        }

        private static bool TryToObject(byte[] body, out object obj)
        {
            try
            {
                obj = body.ToObject<object>();
                return true;
            }
            catch
            {
                obj = default;
                return false;
            }
        }

        private static bool TryToObject<T>(byte[] body, out T obj)
        {
            if (TryToObject(body, out object obj2))
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
            OnEnd();
        }
    }
}