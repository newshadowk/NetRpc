using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using NetRpc.Contract;
using Proxy.Grpc;

namespace NetRpc.Grpc
{
    internal sealed class GrpcClientConnection : IClientConnection
    {
        private readonly AsyncLock _sendLock = new();
        private readonly Client _client;
        private readonly ILogger _logger;
        private AsyncDuplexStreamingCall<StreamBuffer, StreamBuffer> _api = null!;
        private readonly WriteOnceBlock<int> _end = new(i => i);

        public GrpcClientConnection(Client client, ILogger logger)
        {
            _client = client;
            _logger = logger;
        }

        public event AsyncEventHandler<EventArgsT<ReadOnlyMemory<byte>>>? ReceivedAsync;

        public event EventHandler<EventArgsT<Exception>>? ReceiveDisconnected;

        private bool _isDispose;

        private static readonly AsyncLock LockDispose = new();

        public async ValueTask DisposeAsync()
        {
            using (await LockDispose.LockAsync())
            {
                if (_isDispose)
                    return;

                try
                {
                    await _api.RequestStream.CompleteAsync();
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "GrpcClientConnection Dispose");
                }

                _isDispose = true;
            }
        }

        public async Task DisposeFinishAsync()
        {
            //before dispose requestStream need to
            //wait 60 second to receive 'completed' from client side.
            await Task.WhenAny(Task.Delay(1000 * 60),
                _end.ReceiveAsync());
        }

        public ConnectionInfo ConnectionInfo => new()
        {
            Port = _client.Port,
            Description = _client.ConnectionDescription,
            Host = _client.Host,
            ChannelType = ChannelType.Grpc
        };

        public async Task SendAsync(ReadOnlyMemory<byte> buffer, bool isEnd = false, bool isPost = false)
        {
            //add a lock here will not slowdown send speed.
            using (await _sendLock.LockAsync())
            {
                var sb = new StreamBuffer {Body = ByteString.CopyFrom(buffer.ToArray())};
                await _api.RequestStream.WriteAsync(sb);
            }

            if (isEnd)
            {
                await DisposeAsync();
            }
        }

#pragma warning disable 1998
        public async Task StartAsync(string? authorizationToken)
#pragma warning restore 1998
        {
            Metadata? headers = null;
            if (authorizationToken != null)
            {
                headers = new Metadata
                {
                    {
                        "Authorization", $"Bearer {authorizationToken}"
                    }
                };
            }

            _api = _client.CallClient.DuplexStreamingServerMethod(headers);
#pragma warning disable 4014
            Task.Run(async () =>
#pragma warning restore 4014
            {
                try
                {
                    while (await _api.ResponseStream.MoveNext(CancellationToken.None))
                    {
                        var byteArray = _api.ResponseStream.Current.Body.ToByteArray();
                        await OnReceivedAsync(new EventArgsT<ReadOnlyMemory<byte>>(new ReadOnlyMemory<byte>(byteArray)));
                    }
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "_api.ResponseStream.MoveNext error");
                    OnReceiveDisconnected(new EventArgsT<Exception>(e));
                }
                finally
                {
                    _end.Post(1);
                }
            });
        }

        private Task OnReceivedAsync(EventArgsT<ReadOnlyMemory<byte>> e)
        {
            return ReceivedAsync.InvokeAsync(this, e);
        }

        private void OnReceiveDisconnected(EventArgsT<Exception> e)
        {
            ReceiveDisconnected?.Invoke(this, e);
        }
    }
}