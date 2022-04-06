using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Proxy.RabbitMQ;

public sealed class CallSession : IDisposable
{
    private readonly IModel _mainChannel;
    private readonly BasicDeliverEventArgs _e;
    private readonly ILogger _logger;
    private readonly QueueWatcher _queueWatcher;
    private readonly string _serviceToClientQueue;
    private volatile bool _disposed;
    private readonly bool _isPost;
    private volatile string? _consumerTag;
    private readonly AsyncLock _lock_Receive = new();

    public event AsyncEventHandler<EventArgsT<ReadOnlyMemory<byte>>>? ReceivedAsync;

    public event EventHandler? Disconnected;

    public CallSession(IModel mainChannel, QueueWatcher queueWatcher, BasicDeliverEventArgs e, ILogger logger)
    {
        _isPost = e.BasicProperties.ReplyTo == null;
        _serviceToClientQueue = e.BasicProperties.ReplyTo!;
        _mainChannel = mainChannel;
        _e = e;
        _logger = logger;

        _queueWatcher = queueWatcher;
        _queueWatcher.Disconnected += WatcherDisconnected;
        _queueWatcher.Add(_serviceToClientQueue);
    }

    private void WatcherDisconnected(object? sender, EventArgsT<string> e)
    {
        if (e.Value != _serviceToClientQueue)
            return;

        OnDisconnected();
    }

    public bool Start()
    {
        if (!_isPost)
        {
            if (!DeclareCallBack())
                return false;
        }

#pragma warning disable CS4014
        //on exception here
        OnReceivedAsync(new EventArgsT<ReadOnlyMemory<byte>>(_e.Body));
#pragma warning restore CS4014

        return true;
    }

    public void Send(ReadOnlyMemory<byte> buffer)
    {
        _mainChannel.BasicPublish("", _serviceToClientQueue, null!, buffer);
    }

    private bool DeclareCallBack()
    {
        try
        {
            var clientToServiceQueue = _mainChannel.QueueDeclare().QueueName;
            Console.WriteLine($"service: _clientToServiceQueue: {clientToServiceQueue}");
            var clientToServiceConsumer = new AsyncEventingBasicConsumer(_mainChannel);
            clientToServiceConsumer.Received += (_, e) => OnReceivedAsync(new EventArgsT<ReadOnlyMemory<byte>>(e.Body));
            _consumerTag = _mainChannel.BasicConsume(clientToServiceQueue, true, clientToServiceConsumer);
            Send(Encoding.UTF8.GetBytes(clientToServiceQueue));
            return true;
        }
        catch
        {
            _logger.LogWarning("DeclareCallBack failed.");
            return false;
        }
    }

    private async Task OnReceivedAsync(EventArgsT<ReadOnlyMemory<byte>> e)
    {
        //Consumer will has 2 threads invoke simultaneously.
        //lock here make sure the msg sequence
        using (await _lock_Receive.LockAsync())
            await ReceivedAsync.InvokeAsync(this, e);
    }

    private void OnDisconnected()
    {
        Disconnected?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        _queueWatcher.Disconnected -= WatcherDisconnected;
        _queueWatcher.Remove(_serviceToClientQueue);

        _mainChannel.TryBasicAck(_e.DeliveryTag, false, _logger);
        _mainChannel.TryBasicCancel(_consumerTag, _logger);
    }
}