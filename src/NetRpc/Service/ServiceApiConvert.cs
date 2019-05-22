using System;
using System.IO;
using System.Runtime.Serialization;
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

        public Task SendResultAsync(object body, ActionInfo action, object[] args)
        {
            if (body is Stream)
                return SafeSend(new Reply(ReplyType.ResultStream).All);

            byte[] bytes;
            try
            {
                bytes = body.ToBytes();
            }
            catch (Exception e)
            {
                return SendFaultAsync(e, action, args);
            }

            return SafeSend(new Reply(ReplyType.Result, bytes).All);
        }

        public Task SendFaultAsync(Exception body, ActionInfo action, object[] args)
        {
            byte[] bytes;
            try
            {
                var bodyFe = body as FaultException;
                if (bodyFe == null &&
                    !(body is OperationCanceledException))
                {
                    var gt = typeof(FaultException<>).MakeGenericType(body.GetType());
                    var fe = (FaultException)Activator.CreateInstance(gt, body);
                    fe.AppendMethodInfo(action, args);
                    bytes = fe.ToBytes();
                }
                else if (bodyFe != null)
                {
                    bodyFe.AppendMethodInfo(action, args);
                    bytes = bodyFe.ToBytes();
                }
                else
                    bytes = body.ToBytes();
            }
            catch (Exception e)
            {
                var se = new SerializationException($"{e.Message}");
                FaultException<SerializationException> fse = new FaultException<SerializationException>(se);
                return SafeSend(new Reply(ReplyType.Fault, fse.ToBytes()).All);
            }
            return SafeSend(new Reply(ReplyType.Fault, bytes).All);
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

        public Task SendCallbackAsync(object body, ActionInfo action, object[] args)
        {
            byte[] bytes;
            try
            {
                bytes = body.ToBytes();
            }
            catch (Exception e)
            {
                return SendFaultAsync(e, action, args);
            }

            return SafeSend(new Reply(ReplyType.Callback, bytes).All);
        }

        public BufferBlockStream GetRequestStream()
        {
            return new BufferBlockStream(_block);
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