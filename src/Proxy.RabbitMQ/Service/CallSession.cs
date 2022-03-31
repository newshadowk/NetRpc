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
    private readonly IModel _subChannel;
    private readonly QueueWatcher _queueWatcher;
    private readonly string _serviceToClientQueue;
    private string? _clientToServiceQueue;
    private bool _disposed;
    private readonly bool _isPost;
    private volatile string? _consumerTag;
    private readonly AsyncLock _lock_Receive = new();
    private volatile bool _serviceToClientQueueDisconnected;

    public event AsyncEventHandler<EventArgsT<ReadOnlyMemory<byte>>>? ReceivedAsync;

    public event EventHandler? Disconnected;

    public CallSession(IModel mainChannel, IModel subChannel, QueueWatcher queueWatcher, BasicDeliverEventArgs e, ILogger logger)
    {
        _queueWatcher = queueWatcher;
        _isPost = e.BasicProperties.ReplyTo == null;
        _serviceToClientQueue = e.BasicProperties.ReplyTo!;
        _mainChannel = mainChannel;
        _subChannel = subChannel;
        _queueWatcher = queueWatcher;
        _e = e;
        _logger = logger;

        _queueWatcher.Add(_serviceToClientQueue);
        _queueWatcher.Disconnected += QueueWatcherDisconnected;
    }

    private void QueueWatcherDisconnected(object? sender, EventArgsT<string> e)
    {
        if (e.Value == _serviceToClientQueue)
        {
            _serviceToClientQueueDisconnected = true;
            OnDisconnected();
        }
    }

    public async void Start()
    {
        if (!_isPost)
        {
            _clientToServiceQueue = _subChannel.QueueDeclare().QueueName;
            Console.WriteLine($"service: _clientToServiceQueue: {_clientToServiceQueue}");
            var clientToServiceConsumer = new AsyncEventingBasicConsumer(_subChannel);
            clientToServiceConsumer.Received += (_, e) => OnReceivedAsync(new EventArgsT<ReadOnlyMemory<byte>>(e.Body));
            _consumerTag = _subChannel.BasicConsume(_clientToServiceQueue, true, clientToServiceConsumer);
            Send(Encoding.UTF8.GetBytes(_clientToServiceQueue));
        }

        await OnReceivedAsync(new EventArgsT<ReadOnlyMemory<byte>>(_e.Body));
    }

    public void Send(ReadOnlyMemory<byte> buffer)
    {
        _subChannel.BasicPublish("", _serviceToClientQueue, null!, buffer);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        _queueWatcher.Disconnected -= QueueWatcherDisconnected;
        _queueWatcher.Remove(_serviceToClientQueue);

        _mainChannel.TryBasicAck(_e.DeliveryTag, false, _logger);
        if (_consumerTag != null && !_serviceToClientQueueDisconnected)
            _subChannel.TryBasicCancel(_consumerTag, _logger);
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
}