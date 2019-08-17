using System;
using System.Threading;
using Grpc.Core;

namespace Grpc.Base
{
    public sealed class Client : IDisposable
    {
        private readonly Channel _channel;
        private volatile bool _disposed;

        public MessageCall.MessageCallClient CallClient { get; private set; }

        public Client(Channel channel)
        {
            _channel = channel;
        }

        public void Connect()
        {
            CallClient = new MessageCall.MessageCallClient(_channel);
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _channel?.ShutdownAsync().Wait();
            _disposed = true;
        }
    }
}