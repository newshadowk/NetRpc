using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NetRpc.Contract;
using Proxy.RabbitMQ;
using RabbitMQ.Client;

namespace NetRpc.RabbitMQ;

public class RabbitMQClientConnection : IClientConnection
{
    private readonly IConnection _mainConnection;
    private readonly MQOptions _opt;
    private readonly RabbitMQOnceCall _call;

    public RabbitMQClientConnection(IConnection mainConnection, IConnection subConnection, IModel mainChannel, MainWatcher mainWatcher, SubWatcher subWatcher, MQOptions opt, ILogger logger)
    {
        _opt = opt;
        _call = new RabbitMQOnceCall(subConnection, mainChannel, mainWatcher, subWatcher, opt.RpcQueue, opt.FirstReplyTimeOut, logger);
        _call.ReceivedAsync += CallReceived;
        _call.Disconnected += CallDisconnected;
        _mainConnection = mainConnection;
        _mainConnection.ConnectionShutdown += MainConnectionShutdown;
    }

    private void CallDisconnected(object? sender, EventArgs e)
    {
        OnReceiveDisconnected(new EventArgsT<string>("Call QueueWatcher Disconnected"));
    }

    private void MainConnectionShutdown(object? sender, ShutdownEventArgs e)
    {
        OnReceiveDisconnected(new EventArgsT<string>($"cmdConn shutdown, ReplyCode:{e.ReplyCode}, ReplyText:{e.ReplyText}, ClassId:{e.ClassId}, MethodId:{e.MethodId}"));
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
        _mainConnection.ConnectionShutdown -= MainConnectionShutdown;
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

    public Task SendAsync(ReadOnlyMemory<byte> buffer, bool isEnd = false, bool isPost = false, byte mqPriority = 0)
    {
        return _call.SendAsync(buffer, isPost, mqPriority);
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