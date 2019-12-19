using System;
using Grpc.Core;

namespace Grpc.Base
{
#if NETSTANDARD2_1 || NETCOREAPP3_1
    public class Client : IDisposable, IAsyncDisposable
#else
    public sealed class Client : IDisposable
#endif
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

#if NETSTANDARD2_1 || NETCOREAPP3_1
        public async System.Threading.Tasks.ValueTask DisposeAsync()
        {
            if (_disposed)
                return;
            await _channel?.ShutdownAsync();
            _disposed = true;
        }
#endif
    }
}