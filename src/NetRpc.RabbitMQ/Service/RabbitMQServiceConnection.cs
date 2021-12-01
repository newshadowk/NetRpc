using System;
using System.Threading.Tasks;
using RabbitMQ.Base;

namespace NetRpc.RabbitMQ;

internal sealed class RabbitMQServiceConnection : IServiceConnection
{
    private readonly CallSession _callSession;

    public RabbitMQServiceConnection(CallSession callSession)
    {
        _callSession = callSession;
        _callSession.ReceivedAsync += (_, e) => OnReceivedAsync(new EventArgsT<ReadOnlyMemory<byte>>(e.Value));
    }

    public ValueTask DisposeAsync()
    {
        _callSession.Dispose();
        return new ();
    }

    public event AsyncEventHandler<EventArgsT<ReadOnlyMemory<byte>>>? ReceivedAsync;

    public Task SendAsync(ReadOnlyMemory<byte> buffer)
    {
        return Task.Run(() => { _callSession.Send(buffer); });
    }

    public Task StartAsync()
    {
        _callSession.Start();
        return Task.CompletedTask;
    }

    private Task OnReceivedAsync(EventArgsT<ReadOnlyMemory<byte>> e)
    {
        return ReceivedAsync.InvokeAsync(this, e);
    }
}