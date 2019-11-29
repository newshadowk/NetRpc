using System;
using System.IO;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Logging;

namespace NetRpc
{
    internal sealed class BufferServiceOnceApiConvert : IServiceOnceApiConvert
    {
        private readonly IServiceConnection _connection;
        private readonly ILogger _logger;
        private CancellationTokenSource _cts;

        private readonly BufferBlock<(byte[], BufferType)> _block =
            new BufferBlock<(byte[], BufferType)>(new DataflowBlockOptions {BoundedCapacity = Helper.StreamBufferCount});

        private readonly WriteOnceBlock<Request> _cmdReq = new WriteOnceBlock<Request>(null);

        public BufferServiceOnceApiConvert(IServiceConnection connection, ILogger logger)
        {
            _connection = connection;
            _logger = logger;
        }

        public async Task StartAsync(CancellationTokenSource cts)
        {
            _cts = cts;
            _connection.Received += ConnectionReceived;
            await _connection.StartAsync();
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

        public async Task<bool> SendResultAsync(CustomResult result, Stream stream, string streamName, ActionExecutingContext context)
        {
            if (result.Result is Stream s)
            {
                await SafeSendAsync(Reply.FromResultStream(s.GetLength()));
                return true;
            }

            Reply reply;
            try
            {
                reply = Reply.FromResult(result);
            }
            catch (Exception e)
            {
                await SendFaultAsync(e, context);
                return true;
            }

            await SafeSendAsync(reply);
            return true;
        }

        public Task SendFaultAsync(Exception body, ActionExecutingContext context)
        {
            try
            {
                var warpEx = Helper.WarpException(body, context);
                var reply = Reply.FromFault(warpEx);
                return SafeSendAsync(reply);
            }
            catch (Exception e)
            {
                var se = new SerializationException($"{e.Message}");
                var fse = new FaultException<SerializationException>(se);
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
            catch (Exception e)
            {
                _logger.LogWarning(e, null);
            }
        }

        public void Dispose()
        {
            _connection?.Dispose();
            _cts?.Dispose();
        }

#if NETSTANDARD2_1
        public async ValueTask DisposeAsync()
        {
            if (_connection != null)
                await _connection.DisposeAsync();
            _cts?.Dispose();
        }
#endif
    }
}