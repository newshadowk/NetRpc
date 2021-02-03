using System;
using System.IO;
using System.IO.Pipelines;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Logging;
using NetRpc.Contract;

namespace NetRpc
{
    internal sealed class BufferServiceOnceApiConvert : IServiceOnceApiConvert
    {
        private readonly IServiceConnection _connection;
        private readonly ILogger _logger;
        private CancellationTokenSource _cts = null!;
        private readonly DuplexPipe _streamPipe = new(new PipeOptions(pauseWriterThreshold: Helper.StreamBufferCacheCount, resumeWriterThreshold: 1));

        private readonly WriteOnceBlock<byte[]> _cmdReq = new(null);

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

        private async Task ConnectionReceivedAsync(object sender, EventArgsT<ReadOnlyMemory<byte>> e)
        {
            var r = new Request(e.Value);

            switch (r.Type)
            {
                case RequestType.Cmd:
                    _cmdReq.Post(r.Body.ToArray());
                    break;
                case RequestType.Buffer:
                    await _streamPipe.OutputStream.ComWriteAsync(r.Body);
                    break;
                case RequestType.BufferEnd:
                    await _streamPipe.OutputStream.ComWriteAsync(r.Body);
                    await _streamPipe.Output.CompleteAsync();
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
            var requestBody = await _cmdReq.ReceiveAsync();
            var ocp = requestBody.ToObject<OnceCallParam>();

            //stream
            ProxyStream? rs;
            if (ocp.HasStream)
            {
                if (ocp.PostStream != null)
                    rs = new ProxyStream(new MemoryStream(ocp.PostStream));
                else
                    rs = new ProxyStream(_streamPipe.InputStream, ocp.StreamLength, true);
            }
            else
                rs = null;

            return new ServiceOnceCallParam(ocp.Action, ocp.PureArgs, ocp.StreamLength, rs, ocp.Header);
        }

        public async Task<bool> SendResultAsync(CustomResult result, Stream? stream, string? streamName, ActionExecutingContext context)
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

        public Task SendFaultAsync(Exception body, ActionExecutingContext? context)
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

        public Task SendBufferAsync(ReadOnlyMemory<byte> buffer)
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
        
        public Task SendCallbackAsync(object? callbackObj)
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

        private async Task SendAsync(Message reply)
        {
            await _connection.SendAsync(reply.All);
        }

        public async ValueTask DisposeAsync()
        {
            await _streamPipe.DisposeAsync();
            _cts.Dispose();
        }
    }
}