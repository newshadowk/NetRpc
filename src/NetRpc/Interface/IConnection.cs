using System;
using System.Threading.Tasks;

namespace NetRpc
{
    public interface IClientConnection : IDisposable
#if NETSTANDARD2_1 || NETCOREAPP3_1
        , IAsyncDisposable
#endif
    {
        ConnectionInfo ConnectionInfo { get; }

        event AsyncEventHandler<EventArgsT<byte[]>> ReceivedAsync;

        event EventHandler<EventArgsT<Exception>> ReceiveDisconnected;

        Task SendAsync(byte[] buffer, bool isEnd = false, bool isPost = false);

        Task StartAsync(string authorizationToken);
    }
    public interface IServiceConnection : IDisposable
#if NETSTANDARD2_1 || NETCOREAPP3_1
        , IAsyncDisposable
#endif
    {
        event AsyncEventHandler<EventArgsT<byte[]>> ReceivedAsync;

        Task SendAsync(byte[] buffer);

        Task StartAsync();
    }
}