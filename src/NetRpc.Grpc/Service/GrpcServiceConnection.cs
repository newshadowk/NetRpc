using System;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Google.Protobuf;
using Proxy.Grpc;
using Grpc.Core;

namespace NetRpc.Grpc
{
    internal sealed class GrpcServiceConnection : IServiceConnection
    {
        private readonly AsyncLock _sendLock = new AsyncLock();
        private readonly IAsyncStreamReader<StreamBuffer> _requestStream;
        private readonly IServerStreamWriter<StreamBuffer> _responseStream;
        private readonly WriteOnceBlock<int> _end = new WriteOnceBlock<int>(i => i);

        public GrpcServiceConnection(IAsyncStreamReader<StreamBuffer> requestStream, IServerStreamWriter<StreamBuffer> responseStream)
        {
            _requestStream = requestStream;
            _responseStream = responseStream;
        }

        public void Dispose()
        {
        }

#if NETSTANDARD2_1 || NETCOREAPP3_1
        public async ValueTask DisposeAsync()
        {
            
        }
#endif

        public async Task AllDisposeAsync()
        {
            //before dispose requestStream need to
            //wait 10 second to receive 'completed' from client side.
            await Task.WhenAny(Task.Delay(10000),
                _end.ReceiveAsync());
        }

        public event EventHandler<EventArgsT<byte[]>> Received;

        public async Task SendAsync(byte[] buffer)
        {
            using (await _sendLock.LockAsync())
                await _responseStream.WriteAsync(new StreamBuffer {Body = ByteString.CopyFrom(buffer)});
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