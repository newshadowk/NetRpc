using System;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Base;
using Client = Grpc.Base.Client;

namespace NetRpc.Grpc
{
    public class ClientConnection : IConnection
    {
        private readonly Client _client;
        private AsyncDuplexStreamingCall<StreamBuffer, StreamBuffer> _api;

        public ClientConnection(Client client)
        {
            _client = client;
        }

        public void Dispose()
        {
            try
            {
                _api?.RequestStream?.CompleteAsync();
            }
            catch
            {
            }
        }

        public event EventHandler<EventArgsT<byte[]>> Received;

        public async Task Send(byte[] buffer)
        {
            await _api.RequestStream.WriteAsync(new StreamBuffer { Body = ByteString.CopyFrom(buffer) });
        }

        public void Start()
        {
            _api = _client.CallClient.DuplexStreamingServerMethod();
            Task.Run(async () =>
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