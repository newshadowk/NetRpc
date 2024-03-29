﻿using Proxy.RabbitMQ;
using AsyncEventHandler = System.AsyncEventHandler;

namespace NetRpc.RabbitMQ;

internal sealed class RabbitMQServiceConnection : IServiceConnection
{
    private readonly CallSession _callSession;

    public RabbitMQServiceConnection(CallSession callSession)
    {
        _callSession = callSession;
        _callSession.ReceivedAsync += (_, e) => OnReceivedAsync(new EventArgsT<ReadOnlyMemory<byte>>(e.Value));
        _callSession.Disconnected += CallSessionDisconnected;
    }

    private void CallSessionDisconnected(object? sender, EventArgs e)
    {
        OnDisconnectedAsync();
    }

    public ValueTask DisposeAsync()
    {
        _callSession.Dispose();
        return new ValueTask();
    }

    public event System.AsyncEventHandler<EventArgsT<ReadOnlyMemory<byte>>>? ReceivedAsync;

    public event AsyncEventHandler? DisconnectedAsync;

    public Task SendAsync(ReadOnlyMemory<byte> buffer)
    {
        return _callSession.SendAsync(buffer);
    }

    public Task<bool> StartAsync()
    {
        return Task.FromResult(_callSession.Start());
    }

    private Task OnReceivedAsync(EventArgsT<ReadOnlyMemory<byte>> e)
    {
        return ReceivedAsync.InvokeAsync(this, e);
    }

    private Task OnDisconnectedAsync()
    {
        return DisconnectedAsync.InvokeAsync(this, EventArgs.Empty);
    }
}