using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NetRpc.Contract;
using Proxy.RabbitMQ;
using RabbitMQ.Client;

namespace NetRpc.RabbitMQ;

public class RabbitMQClientConnection : IClientConnection
{
    private readonly MQConnection _conn;
    private readonly MQOptions _opt;
    private readonly RabbitMQOnceCall _call;

    public RabbitMQClientConnection(ClientConnection conn)
    {
        _conn = conn;
        _opt = conn.Options;
        _call = new RabbitMQOnceCall(conn);
        _call.ReceivedAsync += CallReceived;
        _call.Disconnected += CallDisconnected;
        _conn.Connection.ConnectionShutdown += MainConnectionShutdown;
    }

    private void CallDisconnected(object? sender, EventArgs e)
    {
        OnReceiveDisconnected(new EventArgsT<string>("Call QueueWatcher Disconnected"));
    }

    private void MainConnectionShutdown(object? sender, ShutdownEventArgs e)
    {
        OnReceiveDisconnected(new EventArgsT<string>($"MainConnection Shutdown, ReplyCode:{e.ReplyCode}, ReplyText:{e.ReplyText}"));
    }

    private async Task CallReceived(object sender, Proxy.RabbitMQ.EventArgsT<ReadOnlyMemory<byte>?> e)
    {
        if (e.Value == null)
            await OnReceivedAsync(new EventArgsT<ReadOnlyMemory<byte>>(NullReply.All));
        else
            await OnReceivedAsync(new EventArgsT<ReadOnlyMemory<byte>>(e.Value.Value));
    }

    public ValueTask DisposeAsync()
    {
        _conn.Connection.ConnectionShutdown -= MainConnectionShutdown;
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

    public event System.AsyncEventHandler<EventArgsT<ReadOnlyMemory<byte>>>? ReceivedAsync;

    public event EventHandler<EventArgsT<string>>? ReceiveDisconnected;

    public async Task SendAsync(ReadOnlyMemory<byte> buffer, bool isEnd = false, bool isPost = false, byte mqPriority = 0)
    {
        try
        {
            await _call.SendAsync(buffer, isPost, mqPriority);
        }
        catch (MqHandshakeInnerException e)
        {
            throw new MqHandshakeException(e.QueueCount);
        }
        catch (MaxQueueCountInnerException e)
        {
            throw new MaxQueueCountException(e.QueueCount);
        }
    }

    public Task StartAsync(Dictionary<string, object?> headers, bool isPost)
    {
        _call.Start(isPost);
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