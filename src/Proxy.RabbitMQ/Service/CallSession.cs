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
    private readonly ReceiveWatcher _receiveWatcher;
    private readonly string _serviceToClientQueue;
    private volatile bool _disposed;
    private readonly bool _isPost;
    private volatile string? _consumerTag;
    private readonly AsyncLock _lock_Receive = new();
    private volatile bool _serviceToClientQueueDisconnected;

    public event AsyncEventHandler<EventArgsT<ReadOnlyMemory<byte>>>? ReceivedAsync;

    public event EventHandler? Disconnected;

    public CallSession(IModel mainChannel, IModel subChannel, BasicDeliverEventArgs e, ILogger logger)
    {
        _isPost = e.BasicProperties.ReplyTo == null;
        _serviceToClientQueue = e.BasicProperties.ReplyTo!;
        _mainChannel = mainChannel;
        _subChannel = subChannel;
        _e = e;
        _logger = logger;

        _receiveWatcher = new ReceiveWatcher(subChannel);
        _receiveWatcher.Disconnected += ReceiveWatcherDisconnected;
    }

    private void ReceiveWatcherDisconnected(object? sender, EventArgs e)
    {
        _serviceToClientQueueDisconnected = true;
        OnDisconnected();
    }

    public async void Start()
    {
        if (!_isPost)
        {
            var heartBeatQueue = _receiveWatcher.Start();
            var clientToServiceQueue = _subChannel.QueueDeclare().QueueName;
            Console.WriteLine($"service: _clientToServiceQueue: {clientToServiceQueue}");
            Console.WriteLine($"service: _heartBeatQueue: {heartBeatQueue}");
            var clientToServiceConsumer = new AsyncEventingBasicConsumer(_subChannel);
            clientToServiceConsumer.Received += (_, e) => OnReceivedAsync(new EventArgsT<ReadOnlyMemory<byte>>(e.Body));
            _consumerTag = _subChannel.BasicConsume(clientToServiceQueue, true, clientToServiceConsumer);
            Send(Encoding.UTF8.GetBytes(clientToServiceQueue + "," + heartBeatQueue));
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

        _receiveWatcher.Disconnected -= ReceiveWatcherDisconnected;
        _receiveWatcher.Dispose();

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