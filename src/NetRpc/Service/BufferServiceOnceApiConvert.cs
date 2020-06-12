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
            new BufferBlock<(byte[], BufferType)>(new DataflowBlockOptions {BoundedCapacity = Helper.StreamBufferCacheCount});

        private readonly WriteOnceBlock<Request> _cmdReq = new WriteOnceBlock<Request>(null);

        public BufferServiceOnceApiConvert(IServiceConnection connection, ILogger logger)
        {
            _connection = connection;
            _logger = logger;
        }

        public async Task StartAsync(CancellationTokenSource cts)
        {
            _cts = cts;
            _connection.ReceivedAsync += ConnectionReceivedAsync;
            await _connection.StartAsync();
        }

        private async Task ConnectionReceivedAsync(object sender, EventArgsT<byte[]> e)
        {
            var r = new Request(e.Value);
            switch (r.Type)
            {
                case RequestType.Cmd:
                    _cmdReq.Post(new Request(e.Value));
                    break;
                case RequestType.Buffer:
                    await _block.SendAsync((r.Body, BufferType.Buffer));
                    break;
                case RequestType.BufferEnd:
                    await _block.SendAsync((r.Body, BufferType.End));
                    break;
                case RequestType.Cancel:
                    _cts.Cancel();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public async Task<ServiceOnceCallParam> GetServiceOnceCallParamAsync()
        {
            var r = await _cmdReq.ReceiveAsync();
            var ocp = r.Body.ToObject<OnceCallParam>();

            //stream
            ReadStream rs;
            if (ocp.HasStream)
            {
                if (ocp.PostStream != null)
                    rs = new ProxyStream(new MemoryStream(ocp.PostStream));
                else
                    rs = new BufferBlockStream(_block, ocp.StreamLength);
            }
            else
                rs = null;

            return new ServiceOnceCallParam(ocp.Action, ocp.PureArgs, ocp.StreamLength, rs, ocp.Header);
        }

        public async Task<bool> SendResultAsync(CustomResult result, Stream stream, string streamName, ActionExecutingContext context)
        {
            if (result.Result is Stream s)
            {
                await SendAsync(Reply.FromResultStream(s.GetLength()));
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

            await SendAsync(reply);
            return true;
        }

        public Task SendFaultAsync(Exception body, ActionExecutingContext context)
        {
            try
            {
                var warpEx = Helper.WarpException(body, context);
                var reply = Reply.FromFault(warpEx);
                return SendAsync(reply);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "SendFaultAsync error");
                var se = new SerializationException($"{e.Message}");
                var fse = new FaultException<SerializationException>(se);
                return SendAsync(Reply.FromFault(fse));
            }
        }

        public Task SendBufferAsync(byte[] buffer)
        {
            return SendAsync(Reply.FromBuffer(buffer));
        }

        public Task SendBufferEndAsync()
        {
            return SendAsync(Reply.FromBufferEnd());
        }

        public Task SendBufferCancelAsync()
        {
            return SendAsync(Reply.FromBufferCancel());
        }

        public Task SendBufferFaultAsync()
        {
            return SendAsync(Reply.FromBufferFault());
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
                _logger.LogWarning(e, "SendCallbackAsync error");
                return SendFaultAsync(e, null);
            }

            return SendAsync(reply);
        }

        private async Task SendAsync(Reply reply)
        {
            await _connection.SendAsync(reply.All);
        }

        public void Dispose()
        {
            _connection?.Dispose();
            _cts?.Dispose();
        }

#if NETSTANDARD2_1 || NETCOREAPP3_1
        public async ValueTask DisposeAsync()
        {
            if (_connection != null)
                await _connection.DisposeAsync();
            _cts?.Dispose();
        }
#endif
    }
}