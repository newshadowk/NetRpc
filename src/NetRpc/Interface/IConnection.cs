using System;
using System.Threading.Tasks;

namespace NetRpc
{
    public interface IClientConnection : IAsyncDisposable
    {
        ConnectionInfo ConnectionInfo { get; }

        event AsyncEventHandler<EventArgsT<ReadOnlyMemory<byte>>>? ReceivedAsync;

        event EventHandler<EventArgsT<Exception>>? ReceiveDisconnected;

        Task SendAsync(ReadOnlyMemory<byte> buffer, bool isEnd = false, bool isPost = false);

        Task StartAsync(string? authorizationToken);
    }

    public interface IServiceConnection
    {
        ValueTask DisposeAsync();

        event AsyncEventHandler<EventArgsT<ReadOnlyMemory<byte>>> ReceivedAsync;

        Task SendAsync(ReadOnlyMemory<byte> buffer);

        Task StartAsync();
    }
}