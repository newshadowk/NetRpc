using System;
using Grpc.Net.Client;

namespace Proxy.Grpc
{
    public sealed class Client : IDisposable
    {
        private readonly GrpcChannel _channel;
        private volatile bool _disposed;

        public MessageCall.MessageCallClient CallClient { get; private set; } = null!;

        public Client(GrpcChannelOptions options, string url, string host, int port, string connectionDescription)
        {
            _channel = GrpcChannel.ForAddress(url!, options);
            Host = host;
            Port = port;
            ConnectionDescription = connectionDescription;
        }

        public string Host { get; }

        public int Port { get; }

        public string ConnectionDescription { get; }

        public void Connect()
        {
            CallClient = new MessageCall.MessageCallClient(_channel);
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;

            _channel.Dispose();
        }
    }
}