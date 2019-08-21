﻿using System;
using System.IO;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace NetRpc
{
    internal sealed class BufferServiceOnceApiConvert : IBufferServiceOnceApiConvert
    {
        private readonly IServiceConnection _connection;
        private CancellationTokenSource _cts;

        private readonly BufferBlock<(byte[], BufferType)> _block =
            new BufferBlock<(byte[], BufferType)>(new DataflowBlockOptions {BoundedCapacity = Helper.StreamBufferCount});

        private readonly WriteOnceBlock<Request> _cmdReq = new WriteOnceBlock<Request>(null);

        public BufferServiceOnceApiConvert(IServiceConnection connection)
        {
            _connection = connection;
        }

        public void Start(CancellationTokenSource cts)
        {
            _cts = cts;
            _connection.Received += ConnectionReceived;
            _connection.Start();
        }

        private void ConnectionReceived(object sender, EventArgsT<byte[]> e)
        {
            var r = new Request(e.Value);
            switch (r.Type)
            {
                case RequestType.Cmd:
                    _cmdReq.Post(new Request(e.Value));
                    break;
                case RequestType.Buffer:
                    _block.SendAsync((r.Body, BufferType.Buffer)).Wait();
                    break;
                case RequestType.BufferEnd:
                    _block.SendAsync((r.Body, BufferType.End)).Wait();
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
            var r = await _cmdReq.ReceiveAsync();
            return r.Body.ToObject<OnceCallParam>();
        }

        public Task SendResultAsync(CustomResult result, RpcContext context)
        {
            if (result.Result is Stream s)
                return SafeSendAsync(Reply.FromResultStream(s.GetLength()));

            Reply reply;
            try
            {
                reply = Reply.FromResult(result);
            }
            catch (Exception e)
            {
                return SendFaultAsync(e, context);
            }

            return SafeSendAsync(reply);
        }

        public Task SendFaultAsync(Exception body, RpcContext context)
        {
            try
            {
                Reply reply;
                var bodyFe = body as FaultException;
                if (bodyFe == null && !(body is OperationCanceledException))
                {
                    var gt = typeof(FaultException<>).MakeGenericType(body.GetType());
                    var fe = (FaultException) Activator.CreateInstance(gt, body);
                    fe.AppendMethodInfo(context.ActionInfo, context.Args);
                    reply = Reply.FromFault(fe);
                }
                else if (bodyFe != null)
                {
                    bodyFe.AppendMethodInfo(context.ActionInfo, context.Args);
                    reply = Reply.FromFault(bodyFe);
                }
                else
                    reply = Reply.FromFault(body);

                return SafeSendAsync(reply);
            }
            catch (Exception e)
            {
                var se = new SerializationException($"{e.Message}");
                FaultException<SerializationException> fse = new FaultException<SerializationException>(se);
                return SafeSendAsync(Reply.FromFault(fse));
            }
        }

        public Task SendBufferAsync(byte[] buffer)
        {
            return SafeSendAsync(Reply.FromBuffer(buffer));
        }

        public Task SendBufferEndAsync()
        {
            return SafeSendAsync(Reply.FromBufferEnd());
        }

        public Task SendBufferCancelAsync()
        {
            return SafeSendAsync(Reply.FromBufferCancel());
        }

        public Task SendBufferFaultAsync()
        {
            return SafeSendAsync(Reply.FromBufferFault());
        }

        public Task SendCallbackAsync(object callbackObj)
        {
            Reply reply;
            try
            {
                reply = Reply.FromCallback(callbackObj);
            }
            catch (Exception e)
            {
                return SendFaultAsync(e, null);
            }

            return SafeSendAsync(reply);
        }

        public Stream GetRequestStream(long? length)
        {
            return new BufferBlockStream(_block, length);
        }

        private async Task SafeSendAsync(Reply reply)
        {
            try
            {
                await _connection.SendAsync(reply.All);
            }
            catch
            {
            }
        }

        public void Dispose()
        {
            _connection?.Dispose();
            _cts?.Dispose();
        }
    }
}