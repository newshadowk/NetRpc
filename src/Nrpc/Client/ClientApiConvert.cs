using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Nrpc
{
    internal sealed class ClientApiConvert
    {
        private readonly IConnection _transfer;

        private readonly BufferBlock<(byte[], BufferType)> _block = new BufferBlock<(byte[], BufferType)>();

        public event EventHandler ResultStream;
        public event EventHandler End;
        public event EventHandler<ResultEventArgs> Result;
        public event EventHandler<EventArgsT<object>> Callback;
        public event EventHandler<EventArgsT<object>> Fault;

        public ClientApiConvert(IConnection transfer)
        {
            _transfer = transfer;
            transfer.Received += TransferReceived;
        }

        public void Start()
        {
            _transfer.Start();
        }

        private void TransferReceived(object sender, EventArgsT<byte[]> e)
        {
            var r = new Reply(e.Value);
            switch (r.Type)
            {
                case ReplyType.ResultStream:
                    OnResultStream();
                    break;
                case ReplyType.Result:
                {
                    if (TryToObject(r.Body, out object obj))
                    {
                        var hasStream = obj.HasStream();
                        OnResult(new ResultEventArgs(hasStream, obj));
                        if (!hasStream)
                            OnEnd();
                    }
                    else
                        OnFaultSerializationException();
                    break;
                }
                case ReplyType.Callback:
                {
                    if (TryToObject(r.Body, out object obj))
                        OnCallback(new EventArgsT<object>(obj));
                    else
                        OnFaultSerializationException();
                    break;
                }
                case ReplyType.Fault:
                {
                    if (TryToObject(r.Body, out object obj))
                    {
                        OnFault(new EventArgsT<object>(obj));
                        OnEnd();
                    }
                    else
                        OnFaultSerializationException();
                    break;
                }
                case ReplyType.Buffer:
                    _block.Post((r.Body, BufferType.Buffer));
                    break;
                case ReplyType.BufferCancel:
                    _block.Post((default, BufferType.Cancel));
                    OnEnd();
                    break;
                case ReplyType.BufferFault:
                    _block.Post((default, BufferType.Fault));
                    OnEnd();
                    break;
                case ReplyType.BufferEnd:
                    _block.Post((r.Body, BufferType.End));
                    OnEnd();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public Task SendCancelAsync()
        {
            return _transfer.Send(new Request(RequestType.Cancel).All);
        }

        public Task SendBufferAsync(byte[] body)
        {
            return _transfer.Send(new Request(RequestType.Buffer, body).All);
        }

        public Task SendBufferEndAsync()
        {
            return _transfer.Send(new Request(RequestType.BufferEnd).All);
        }

        public Task SendCmdAsync(OnceCallParam body)
        {
            return _transfer.Send(new Request(RequestType.Cmd, body.ToBytes()).All);
        }

        public BufferBlockStream GetRequestStream()
        {
            return new BufferBlockStream(_block);
        }

        private void OnResult(ResultEventArgs e)
        {
            Result?.Invoke(this, e);
        }

        private void OnCallback(EventArgsT<object> e)
        {
            Callback?.Invoke(this, e);
        }

        private void OnFault(EventArgsT<object> e)
        {
            Fault?.Invoke(this, e);
        }

        private void OnResultStream()
        {
            ResultStream?.Invoke(this, EventArgs.Empty);
        }

        private void OnEnd()
        {
            End?.Invoke(this, EventArgs.Empty);
        }

        private bool TryToObject(byte[] body, out object obj)
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

        private void OnFaultSerializationException()
        {
            OnFault(new EventArgsT<object>(new SerializationException("Deserialization failure when receive data.")));
            OnEnd();
        }
    }
}