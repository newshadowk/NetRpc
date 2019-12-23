using System;

#if NETCOREAPP3_1
using Channel = Grpc.Net.Client.GrpcChannel;
#else
using Channel = Grpc.Core.Channel;
#endif

namespace Proxy.Grpc
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

#if NETSTANDARD2_1 || NETCOREAPP3_1
        public async System.Threading.Tasks.ValueTask DisposeAsync()
        {
            if (_disposed)
                return;
#if NETCOREAPP3_1
            _channel?.Dispose();
#else
            await _channel?.ShutdownAsync();
#endif
            _disposed = true;
        }
#endif
        public void Dispose()
        {
            if (_disposed)
                return;

#if NETCOREAPP3_1
            _channel?.Dispose();
#else
            _channel?.ShutdownAsync().Wait();
#endif
            _disposed = true;
        }
    }
}