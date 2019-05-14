using System;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Base;

namespace NetRpc.Grpc
{
    internal sealed class ServiceConnection : IConnection
    {
        private readonly IAsyncStreamReader<StreamBuffer> _requestStream;
        private readonly IServerStreamWriter<StreamBuffer> _responseStream;

        public ServiceConnection(IAsyncStreamReader<StreamBuffer> requestStream, IServerStreamWriter<StreamBuffer> responseStream)
        {
            _requestStream = requestStream;
            _responseStream = responseStream;
        }

        public void Dispose()
        {
            _requestStream?.Dispose();
        }

        public event EventHandler<EventArgsT<byte[]>> Received;

        public Task Send(byte[] buffer)
        {
            return _responseStream.WriteAsync(new StreamBuffer {Body = ByteString.CopyFrom(buffer)});
        }

        public void Start()
        {
            Task.Run(async () =>
            {
                //MoveNext will have a Exception when client is disconnected.
                while (await _requestStream.MoveNext(CancellationToken.None))
                    OnReceived(new EventArgsT<byte[]>(_requestStream.Current.Body.ToByteArray()));
            });
        }

        private void OnReceived(EventArgsT<byte[]> e)
        {
            Received?.Invoke(this, e);
        }
    }
}