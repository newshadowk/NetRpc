using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NetRpc;

public interface IClientConnection : IAsyncDisposable
{
    ConnectionInfo ConnectionInfo { get; }

    event AsyncEventHandler<EventArgsT<ReadOnlyMemory<byte>>>? ReceivedAsync;

    event EventHandler<EventArgsT<Exception>>? ReceiveDisconnected;

    Task SendAsync(ReadOnlyMemory<byte> buffer, bool isEnd = false, bool isPost = false,byte mqPriority = 0);

    Task StartAsync(Dictionary<string, object?> headers);
}

public interface IServiceConnection : IAsyncDisposable
{
    event AsyncEventHandler<EventArgsT<ReadOnlyMemory<byte>>> ReceivedAsync;

    Task SendAsync(ReadOnlyMemory<byte> buffer);

    Task StartAsync();
}