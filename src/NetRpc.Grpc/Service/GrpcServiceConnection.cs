using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Google.Protobuf;
using Proxy.Grpc;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace NetRpc.Grpc
{
    internal sealed class GrpcServiceConnection : IServiceConnection
    {
        private readonly AsyncLock _sendLock = new AsyncLock();
        private readonly IAsyncStreamReader<StreamBuffer> _requestStream;
        private readonly IServerStreamWriter<StreamBuffer> _responseStream;
        private readonly ILogger _logger;
        private readonly WriteOnceBlock<int> _end = new WriteOnceBlock<int>(i => i);

        public GrpcServiceConnection(IAsyncStreamReader<StreamBuffer> requestStream, IServerStreamWriter<StreamBuffer> responseStream, ILogger logger)
        {
            _requestStream = requestStream;
            _responseStream = responseStream;
            _logger = logger;
        }

        public void Dispose()
        {
        }

#if NETSTANDARD2_1 || NETCOREAPP3_1
        public ValueTask DisposeAsync()
        {
            return new ValueTask();
        }
#endif

        public async Task AllDisposeAsync()
        {
            //before dispose requestStream need to
            //wait 60 second to receive 'completed' from client side.
            await Task.WhenAny(Task.Delay(1000*60),
                _end.ReceiveAsync());
        }

        public event EventHandler<EventArgsT<byte[]>> Received;

        public async Task SendAsync(byte[] buffer)
        {
            //add a lock here will not slowdown send speed.
            using (await _sendLock.LockAsync())
                await _responseStream.WriteAsync(new StreamBuffer { Body = ByteString.CopyFrom(buffer) });
        }

        public Task StartAsync()
        {
            Task.Run(async () =>
            {
                //MoveNext will have a Exception when client is disconnected.
                try
                {
                    while (await _requestStream.MoveNext(CancellationToken.None))
                        OnReceived(new EventArgsT<byte[]>(_requestStream.Current.Body.ToByteArray()));
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, null);
                }
                finally
                {
                    _end.Post(1);
                }
            });

            return Task.CompletedTask;
        }

        private void OnReceived(EventArgsT<byte[]> e)
        {
            Received?.Invoke(this, e);
        }
    }
}