using System;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Grpc.Base;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace NetRpc.Grpc
{
    public class GrpcClientConnection : IClientConnection
    {
        private readonly Client _client;
        private readonly ILogger _logger;
        private AsyncDuplexStreamingCall<StreamBuffer, StreamBuffer> _api;

        public GrpcClientConnection(Client client, ILogger logger)
        {
            _client = client;
            _logger = logger;
        }

        public void Dispose()
        {
            try
            {
                _api?.RequestStream?.CompleteAsync().Wait();
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, null);
            }
        }

#if NETSTANDARD2_1
        public async ValueTask DisposeAsync()
        {
            try
            {
                if (_api?.RequestStream != null) 
                    await _api.RequestStream.CompleteAsync();
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, null);
            }
        }
#endif

        public event EventHandler<EventArgsT<byte[]>> Received;

        public async Task SendAsync(byte[] buffer, bool isPost = false)
        {
            await _api.RequestStream.WriteAsync(new StreamBuffer {Body = ByteString.CopyFrom(buffer)});
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
                while (await _api.ResponseStream.MoveNext(CancellationToken.None))
                    OnReceived(new EventArgsT<byte[]>(_api.ResponseStream.Current.Body.ToByteArray()));
            });
        }

        protected virtual void OnReceived(EventArgsT<byte[]> e)
        {
            Received?.Invoke(this, e);
        }
    }
}