using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RabbitMQ.Base;

public sealed class CallSession : IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _mainModel;
    private readonly BasicDeliverEventArgs _e;
    private readonly ILogger _logger;

    private IModel? _clientToServiceModel;
    private readonly string _serviceToClientQueue;
    private string? _clientToServiceQueue;
    private bool _disposed;
    private readonly bool _isPost;

    public event AsyncEventHandler<EventArgsT<ReadOnlyMemory<byte>>>? ReceivedAsync;

    public CallSession(IConnection connection, IModel mainModel, BasicDeliverEventArgs e, ILogger logger)
    {
        _isPost = e.BasicProperties.ReplyTo == null;
        _serviceToClientQueue = e.BasicProperties.ReplyTo!;
        _connection = connection;
        _mainModel = mainModel;
        _e = e;
        _logger = logger;
    }

    public void Start()
    {
        if (!_isPost)
        {
            _clientToServiceModel = _connection.CreateModel();
            _clientToServiceQueue = _clientToServiceModel.QueueDeclare().QueueName;
            var clientToServiceConsumer = new AsyncEventingBasicConsumer(_clientToServiceModel);
            clientToServiceConsumer.Received += (_, e) => OnReceivedAsync(new EventArgsT<ReadOnlyMemory<byte>>(e.Body));
            _clientToServiceModel.BasicConsume(_clientToServiceQueue, true, clientToServiceConsumer);
            Send(Encoding.UTF8.GetBytes(_clientToServiceQueue));
        }

        OnReceivedAsync(new EventArgsT<ReadOnlyMemory<byte>>(_e.Body));
    }

    public void Send(ReadOnlyMemory<byte> buffer)
    {
        _clientToServiceModel.BasicPublish("", _serviceToClientQueue, null, buffer);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        try
        {
            _mainModel.BasicAck(_e.DeliveryTag, false);
            if (_clientToServiceModel != null)
            {
                _clientToServiceModel.Close();
                _clientToServiceModel.Dispose();
            }
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, null);
        }
    }

    private Task OnReceivedAsync(EventArgsT<ReadOnlyMemory<byte>> e)
    {
        return ReceivedAsync.InvokeAsync(this, e);
    }
}