using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Proxy.RabbitMQ;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RabbitMQ.Base;

public sealed class CallSession : IDisposable
{
    private readonly IModel _mainChannel;
    private readonly BasicDeliverEventArgs _e;
    private readonly ILogger _logger;
    private readonly IModel _clientToServiceChannel;
    private readonly string _serviceToClientQueue;
    private string? _clientToServiceQueue;
    private bool _disposed;
    private readonly bool _isPost;
    private volatile string? _consumerTag;
    private readonly AsyncLock _lock_Receive = new();

    public event AsyncEventHandler<EventArgsT<ReadOnlyMemory<byte>>>? ReceivedAsync;

    public CallSession(IModel mainChannel, IModel subChannel, BasicDeliverEventArgs e, ILogger logger)
    {
        _isPost = e.BasicProperties.ReplyTo == null;
        _serviceToClientQueue = e.BasicProperties.ReplyTo!;
        _mainChannel = mainChannel;
        _clientToServiceChannel = subChannel;
        _e = e;
        _logger = logger;
    }

    public async void Start()
    {
        if (!_isPost)
        {
            _clientToServiceQueue = _clientToServiceChannel.QueueDeclare().QueueName;
            var clientToServiceConsumer = new AsyncEventingBasicConsumer(_clientToServiceChannel);
            clientToServiceConsumer.Received += (_, e) => OnReceivedAsync(new EventArgsT<ReadOnlyMemory<byte>>(e.Body));
            _consumerTag = _clientToServiceChannel.BasicConsume(_clientToServiceQueue, true, clientToServiceConsumer);
            Send(Encoding.UTF8.GetBytes(_clientToServiceQueue));
        }

        await OnReceivedAsync(new EventArgsT<ReadOnlyMemory<byte>>(_e.Body));
    }

    public void Send(ReadOnlyMemory<byte> buffer)
    {
        _clientToServiceChannel.BasicPublish("", _serviceToClientQueue, null!, buffer);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        _mainChannel.TryBasicAck(_e.DeliveryTag, false, _logger);
        if (_consumerTag != null)
            _clientToServiceChannel.TryBasicCancel(_consumerTag, _logger);
    }

    private async Task OnReceivedAsync(EventArgsT<ReadOnlyMemory<byte>> e)
    {
        //Consumer will has 2 threads invoke simultaneously.
        //lock here make sure the msg sequence
        using (await _lock_Receive.LockAsync())
            await ReceivedAsync.InvokeAsync(this, e);
    }
}