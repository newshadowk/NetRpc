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
        (string serviceToClientQueue, string serviceToClientHeartBeatQueue) = Helper.GetQueueNames(e.BasicProperties.ReplyTo!);
        _serviceToClientQueue = serviceToClientQueue;
        _mainChannel = mainChannel;
        _subChannel = subChannel;
        _e = e;
        _logger = logger;

        _queueWatcher = new QueueWatcher(subChannel, logger);
        _queueWatcher.StartSend(serviceToClientHeartBeatQueue);
        _queueWatcher.Disconnected += ReceiveWatcherDisconnected;
    }

    private void ReceiveWatcherDisconnected(object? sender, EventArgs e)
    {
        _serviceToClientQueueDisconnected = true;
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
        _subChannel.BasicPublish("", _serviceToClientQueue, null!, buffer);
    }

    private bool DeclareCallBack()
    {
        var clientToServcieHeartBeatQueue = _queueWatcher.StartListen();
        if (clientToServcieHeartBeatQueue == null)
            return false;

        try
        {
            var clientToServiceQueue = _subChannel.QueueDeclare().QueueName;
            Console.WriteLine($"service: _clientToServiceQueue: {clientToServiceQueue}");
            var clientToServiceConsumer = new AsyncEventingBasicConsumer(_subChannel);
            clientToServiceConsumer.Received += (_, e) => OnReceivedAsync(new EventArgsT<ReadOnlyMemory<byte>>(e.Body));
            _consumerTag = _subChannel.BasicConsume(clientToServiceQueue, true, clientToServiceConsumer);
            Send(Encoding.UTF8.GetBytes(clientToServiceQueue + "," + clientToServcieHeartBeatQueue));
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

        _queueWatcher.Dispose();

        _mainChannel.TryBasicAck(_e.DeliveryTag, false, _logger);
        if (_consumerTag != null && !_serviceToClientQueueDisconnected)
            _subChannel.TryBasicCancel(_consumerTag, _logger);
    }
}