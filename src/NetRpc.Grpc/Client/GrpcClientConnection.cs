using System;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Proxy.Grpc;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace NetRpc.Grpc
{
    public sealed class GrpcClientConnection : IClientConnection
    {
        private readonly AsyncLock _sendLock = new AsyncLock();
        private readonly Client _client;
        private readonly ILogger _logger;
        private AsyncDuplexStreamingCall<StreamBuffer, StreamBuffer> _api;
        private volatile bool _isEnd;

        public GrpcClientConnection(Client client, ILogger logger)
        {
            _client = client;
            _logger = logger;
        }

        public event Func<object, EventArgsT<byte[]>, Task> ReceivedAsync;

        public event EventHandler<EventArgsT<Exception>> ReceiveDisconnected;

        public void Dispose()
        {
        }

#if NETSTANDARD2_1 || NETCOREAPP3_1
        public async ValueTask DisposeAsync()
        {
        }
#endif

        public ConnectionInfo ConnectionInfo => new ConnectionInfo
        {
            Port = _client.Port,
            Description = _client.ConnectionDescription,
            Host = _client.Host, 
            ChannelType =  ChannelType.Grpc
        };

        public async Task SendAsync(byte[] buffer, bool isEnd = false, bool isPost = false)
        {
            if (_isEnd)
                return;

            //add a lock here will not slowdown send speed.
            using (await _sendLock.LockAsync())
                await _api.RequestStream.WriteAsync(new StreamBuffer {Body = ByteString.CopyFrom(buffer)});

            if (isEnd)
            {
                _isEnd = true;
                await _api.RequestStream.CompleteAsync();
            }
        }

#pragma warning disable 1998
        public async Task StartAsync()
#pragma warning restore 1998
        {
            _api = _client.CallClient.DuplexStreamingServerMethod();
#pragma warning disable 4014
            Task.Run(async () =>
#pragma warning restore 4014
            {
                try
                {
                    while (await _api.ResponseStream.MoveNext(CancellationToken.None))
                        await OnReceivedAsync(new EventArgsT<byte[]>(_api.ResponseStream.Current.Body.ToByteArray()));
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "_api.ResponseStream.MoveNext error");
                    OnReceiveDisconnected(new EventArgsT<Exception>(e));
                }
            });
        }

        private Task OnReceivedAsync(EventArgsT<byte[]> e)
        {
            return ReceivedAsync.InvokeAsync(this, e);
        }

        private void OnReceiveDisconnected(EventArgsT<Exception> e)
        {
            ReceiveDisconnected?.Invoke(this, e);
        }
    }
}