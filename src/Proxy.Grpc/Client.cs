using System;
using System.Threading.Tasks;
using Channel = Grpc.Net.Client.GrpcChannel;

namespace Proxy.Grpc
{
    public class Client : IAsyncDisposable
    {
        private readonly Channel _channel;
        private volatile bool _disposed;

        public MessageCall.MessageCallClient CallClient { get; private set; } = null!;

        public Client(Channel channel, string host, int port, string connectionDescription)
        {
            _channel = channel;
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

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
                return;

            _channel.Dispose();
            _disposed = true;
        }
    }
}