using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace NetRpc
{
    internal sealed class ServiceApiConvert
    {
        private readonly IConnection _transfer;
        private readonly CancellationTokenSource _cts;
        private readonly BufferBlock<(byte[], BufferType)> _block = new BufferBlock<(byte[], BufferType)>();
        private readonly WriteOnceBlock<Request> _cmdWob = new WriteOnceBlock<Request>(null);

        public ServiceApiConvert(IConnection transfer, CancellationTokenSource cts)
        {
            _transfer = transfer;
            _cts = cts;
            _transfer.Received += TransferReceived;
        }

        public void Start()
        {
            _transfer.Start();
        }

        private void TransferReceived(object sender, EventArgsT<byte[]> e)
        {
            var r = new Request(e.Value);
            switch (r.Type)
            {
                case RequestType.Cmd:
                    _cmdWob.Post(new Request(e.Value));
                    break;
                case RequestType.Buffer:
                    _block.Post((r.Body, BufferType.Buffer));
                    break;
                case RequestType.BufferEnd:
                    _block.Post((r.Body, BufferType.End));
                    break;
                case RequestType.Cancel:
                    _cts.Cancel();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public async Task<OnceCallParam> GetOnceCallParamAsync()
        {
            var r = await _cmdWob.ReceiveAsync();
            return r.Body.ToObject<OnceCallParam>();
        }

        public Task SendResultAsync(object body)
        {
            if (body is Stream)
                return SafeSend(new Reply(ReplyType.ResultStream).All);

            var t = CheckSerializable(body);
            if (t != null)
                return t;

            return SafeSend(new Reply(ReplyType.Result, body.ToBytes()).All);
        }

        public Task SendFaultAsync(Exception body)
        {
            var t = CheckSerializable(body);
            if (t != null)
                return t;

            return SafeSend(new Reply(ReplyType.Fault, body.ToBytes()).All);
        }

        public Task SendBufferAsync(byte[] body)
        {
            return SafeSend(new Reply(ReplyType.Buffer, body).All);
        }

        public Task SendBufferEndAsync()
        {
            return SafeSend(new Reply(ReplyType.BufferEnd).All);
        }

        public Task SendBufferCancelAsync()
        {
            return SafeSend(new Reply(ReplyType.BufferCancel).All);
        }

        public Task SendBufferFaultAsync()
        {
            return SafeSend(new Reply(ReplyType.BufferFault).All);
        }

        public Task SendCallbackAsync(object body)
        {
            var t = CheckSerializable(body);
            if (t != null)
                return t;

            return SafeSend(new Reply(ReplyType.Callback, body.ToBytes()).All);
        }

        public BufferBlockStream GetRequestStream()
        {
            return new BufferBlockStream(_block);
        }

        private Task CheckSerializable(object body)
        {
            if (body == null)
                return null;
            if (!body.IsSerializable())
                return SendFaultAsync(new System.Runtime.Serialization.SerializationException($"{body.GetType()} is not serializable."));
            return null;
        }

        private async Task SafeSend(byte[] body)
        {
            try
            {
                await _transfer.Send(body);
            }
            catch
            {
            }
        }
    }
}