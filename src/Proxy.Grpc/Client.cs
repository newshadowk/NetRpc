using System;
using Grpc.Net.Client;

namespace Proxy.Grpc
{
    public sealed class Client : IDisposable
    {
        private readonly GrpcChannel _channel;
        private volatile bool _disposed;

        public MessageCall.MessageCallClient CallClient { get; private set; } = null!;

        public Client(GrpcChannelOptions options, string url, string? headerHost, string connectionDescription)
        {
            Uri uri = new (url);
            _channel = GrpcChannel.ForAddress(url!, options);
            Host = uri.Host;
            Port = uri.Port;
            HeaderHost = headerHost;
            ConnectionDescription = connectionDescription;
        }

        public string Host { get; }

        public string? HeaderHost { get; }

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