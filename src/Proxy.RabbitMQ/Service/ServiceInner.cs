using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RabbitMQ.Base;

public sealed class ServiceInner : IDisposable
{
    public event AsyncEventHandler<EventArgsT<CallSession>>? ReceivedAsync;
    private readonly string _rpcQueue;
    private readonly int _prefetchCount;
    private readonly bool _durable;
    private readonly bool _autoDelete;
    private readonly ILogger _logger;
    private readonly IConnection _connect;
    private volatile IModel? _mainModel;
    private volatile bool _disposed;
    private readonly int _maxPriority;

    public ServiceInner(IConnection connect, string rpcQueue, int prefetchCount, int maxPriority, bool durable, bool autoDelete, ILogger logger)
    {
        _connect = connect;
        _rpcQueue = rpcQueue;
        _prefetchCount = prefetchCount;
        _durable = durable;
        _autoDelete = autoDelete;
        _logger = logger;
        _maxPriority = maxPriority;
    }

    public void CreateChannel()
    {
        _mainModel = _connect.CreateModel();
        var args = new Dictionary<string, object>();
        if (_maxPriority > 0)
            args.Add("x-max-priority", _maxPriority);
        _mainModel.QueueDeclare(_rpcQueue, _durable, false, _autoDelete, args);
        var consumer = new AsyncEventingBasicConsumer(_mainModel);
        _mainModel.BasicQos(0, (ushort) _prefetchCount, true);
        _mainModel.BasicConsume(_rpcQueue, false, consumer);
        consumer.Received +=  (_, e) => OnReceivedAsync(new EventArgsT<CallSession>(new CallSession(_connect, _mainModel, e, _logger)));
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        try
        {
            _mainModel?.Close();
            _mainModel?.Dispose();
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, null);
        }
    }

    private Task OnReceivedAsync(EventArgsT<CallSession> e)
    {
        return ReceivedAsync.InvokeAsync(this, e);
    }
}