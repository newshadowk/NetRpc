namespace NetRpc;

public interface IClientConnection : IAsyncDisposable
{
    ConnectionInfo ConnectionInfo { get; }

    event AsyncEventHandler<EventArgsT<ReadOnlyMemory<byte>>>? ReceivedAsync;

    event EventHandler<EventArgsT<string>>? ReceiveDisconnected;

    Task SendAsync(ReadOnlyMemory<byte> buffer, bool isEnd = false, bool isPost = false, byte mqPriority = 0);

    Task StartAsync(Dictionary<string, object?> headers, bool isPost);
}

public interface IServiceConnection : IAsyncDisposable
{
    event AsyncEventHandler<EventArgsT<ReadOnlyMemory<byte>>> ReceivedAsync;

    event AsyncEventHandler? DisconnectedAsync;

    Task SendAsync(ReadOnlyMemory<byte> buffer);

    Task<bool> StartAsync();
}