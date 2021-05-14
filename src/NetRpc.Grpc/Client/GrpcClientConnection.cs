using System;
using System.Collections.Generic;
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
        private readonly WriteOnceBlock<bool> _end = new(i => i);

        public GrpcClientConnection(Client client, ILogger logger)
        {
            _client = client;
            _logger = logger;
        }

        public event AsyncEventHandler<EventArgsT<ReadOnlyMemory<byte>>>? ReceivedAsync;

        public event EventHandler<EventArgsT<Exception>>? ReceiveDisconnected;

        public event EventHandler? Finished;

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
            //60 second to wait 'MessageCallImpl.DuplexStreamingServerMethod' execute finish from the service side.
            await Task.WhenAny(Task.Delay(1000 * 60), _end.ReceiveAsync());
        }

        public ConnectionInfo ConnectionInfo => new()
        {
            Port = _client.Port,
            Description = _client.ConnectionDescription,
            Host = _client.Host,
            HeadHost = _client.HeaderHost,
            ChannelType = ChannelType.Grpc
        };

        public async Task SendAsync(ReadOnlyMemory<byte> buffer, bool isEnd = false, bool isPost = false,byte mqPriority = 0)
        {
            //add a lock here will not slowdown send speed.
            using (await _sendLock.LockAsync())
            {
                var sb = new StreamBuffer {Body = ByteString.CopyFrom(buffer.ToArray())};
                try
                {
                    await _api.RequestStream.WriteAsync(sb);
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, $"Client WriteAsync error. {_client.ConnectionDescription}");
                    throw;
                }
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
            //create header
            List<(string, string)> headersList = new();
            Metadata? headers = null;
            if (authorizationToken != null) 
                headersList.Add(("Authorization", $"Bearer {authorizationToken}"));
            if (_client.HeaderHost != null) 
                headersList.Add(("Host", _client.HeaderHost));
            if (headersList.Count > 0)
            {
                headers = new Metadata();
                headersList.ForEach(i => headers.Add(i.Item1, i.Item2));
            }

            //create connection.
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
                    _logger.LogWarning(e, $"Client MoveNext error. {_client.ConnectionDescription}");
                    OnReceiveDisconnected(new EventArgsT<Exception>(e));
                }
                finally
                {
                    _end.Post(true);
                    OnFinished();
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

        private void OnFinished()
        {
            Finished?.Invoke(this, EventArgs.Empty);
        }
    }
}