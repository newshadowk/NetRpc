using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NetRpc.Contract;
using RabbitMQ.Base;
using RabbitMQ.Client;

namespace NetRpc.RabbitMQ;

public class RabbitMQClientConnection : IClientConnection
{
    private readonly MQOptions _opt;
    private readonly RabbitMQOnceCall _call;

    public RabbitMQClientConnection(IConnection mainConnection, IModel mainChannel, IModel subChannel, MQOptions opt, ILogger logger)
    {
        _opt = opt;
        _call = new RabbitMQOnceCall(mainChannel, subChannel, opt.RpcQueue, logger);
        _call.ReceivedAsync += CallReceived;
        mainConnection.ConnectionShutdown += mainConnection_ConnectionShutdown;
    }

    private void mainConnection_ConnectionShutdown(object? sender, ShutdownEventArgs e)
    {
        OnReceiveDisconnected(new EventArgsT<string>($"cmdConn shutdown, ReplyCode:{e.ReplyCode}, ReplyText:{e.ReplyText}, ClassId:{e.ClassId}, MethodId:{e.MethodId}"));
    }

    private async Task CallReceived(object sender, global::RabbitMQ.Base.EventArgsT<ReadOnlyMemory<byte>?> e)
    {
        if (e.Value == null)
            await OnReceivedAsync(new EventArgsT<ReadOnlyMemory<byte>>(NullReply.All));
        else
            await OnReceivedAsync(new EventArgsT<ReadOnlyMemory<byte>>(e.Value.Value));
    }

    public ValueTask DisposeAsync()
    {
        _call.Dispose();
        return new ValueTask();
    }

    public ConnectionInfo ConnectionInfo => new()
    {
        Host = _opt.Host,
        Port = _opt.Port,
        Description = _opt.ToString(),
        ChannelType = ChannelType.RabbitMQ
    };

    public event AsyncEventHandler<EventArgsT<ReadOnlyMemory<byte>>>? ReceivedAsync;

    public event EventHandler<EventArgsT<string>>? ReceiveDisconnected;

    public Task SendAsync(ReadOnlyMemory<byte> buffer, bool isEnd = false, bool isPost = false, byte mqPriority = 0)
    {
        return _call.SendAsync(buffer, isPost, mqPriority);
    }

    public Task StartAsync(Dictionary<string, object?> headers)
    {
        _call.CreateChannel();
        return Task.CompletedTask;
    }

    private Task OnReceivedAsync(EventArgsT<ReadOnlyMemory<byte>> e)
    {
        return ReceivedAsync.InvokeAsync(this, e);
    }

    private void OnReceiveDisconnected(EventArgsT<string> e)
    {
        ReceiveDisconnected?.Invoke(this, e);
    }
}