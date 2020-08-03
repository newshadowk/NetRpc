using System;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Proxy.Grpc;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using NetRpc.Contract;

namespace NetRpc.Grpc
{
    internal sealed class GrpcClientConnection : IClientConnection
    {
        private readonly AsyncLock _sendLock = new AsyncLock();
        private readonly Client _client;
        private readonly ILogger _logger;
        private AsyncDuplexStreamingCall<StreamBuffer, StreamBuffer> _api = null!;
        private volatile bool _isEnd;
        private string id = Guid.NewGuid().ToString();

        public GrpcClientConnection(Client client, ILogger logger)
        {
            _client = client;
            _logger = logger;
        }

        public event AsyncEventHandler<EventArgsT<ReadOnlyMemory<byte>>>? ReceivedAsync;

        public event EventHandler<EventArgsT<Exception>>? ReceiveDisconnected;

        public void Dispose()
        {
            W("GrpcClientConnection Dispose");
            

            if (_isEnd)
                return;

            _isEnd = true;
            try
            {
                _api.RequestStream.CompleteAsync();
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "GrpcClientConnection Dispose");
            }
        }

#if NETSTANDARD2_1 || NETCOREAPP3_1
        public async ValueTask DisposeAsync()
        {
            if (_isEnd)
                return;

            _isEnd = true;
            try
            {
                await _api.RequestStream.CompleteAsync();
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "GrpcClientConnection DisposeAsync");
            }
        }
#endif

        public ConnectionInfo ConnectionInfo => new ConnectionInfo
        {
            Port = _client.Port,
            Description = _client.ConnectionDescription,
            Host = _client.Host, 
            ChannelType =  ChannelType.Grpc
        };

        public async Task SendAsync(ReadOnlyMemory<byte> buffer, bool isEnd = false, bool isPost = false)
        {
            W($"sendasync, isend:{isEnd}");
            if (_isEnd)
                return;

            //add a lock here will not slowdown send speed.
            using (await _sendLock.LockAsync())
            {
                var sb = new StreamBuffer { Body = ByteString.CopyFrom(buffer.ToArray()) };
                _logger.LogDebug($"Send count:{Helper.SizeSuffix(sb.Body.Length)}");
                await _api.RequestStream.WriteAsync(sb);
            }

            if (isEnd)
            {
                _isEnd = true;
                W("RequestStream.CompleteAsync");
                await _api.RequestStream.CompleteAsync();
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
                    W($"movenext start, {id}");
                    while (await _api.ResponseStream.MoveNext(CancellationToken.None))
                    {
                        var byteArray = _api.ResponseStream.Current.Body.ToByteArray();
                        W($"movenext:, count, {byteArray.Length}");
                        await OnReceivedAsync(new EventArgsT<ReadOnlyMemory<byte>>(new ReadOnlyMemory<byte>(byteArray)));
                    }
                    W($"movenext end, {id}");
                }
                catch (Exception e)
                {
                    W($"_api.ResponseStream.MoveNext error");
                    _logger.LogWarning(e, "_api.ResponseStream.MoveNext error");
                    OnReceiveDisconnected(new EventArgsT<Exception>(e));
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

        private void W(string s)
        {
            Console.WriteLine($"!!!,{DateTime.Now:MM-dd HH:mm:ss.fff}, {id}, {s}");
        }
    }
}