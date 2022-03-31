using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Proxy.RabbitMQ;

public sealed class Service : IDisposable
{
    public event AsyncEventHandler<EventArgsT<CallSession>>? ReceivedAsync;
    private readonly IConnection _mainConnection;
    private readonly IConnection _subConnection;
    private readonly IModel _mainChannel;
    private readonly IModel _subChannel;
    private readonly string _rpcQueue;
    private readonly int _prefetchCount;
    private readonly int _maxPriority;
    private readonly ILogger _logger;
    private volatile bool _disposed;
    private volatile string? _consumerTag;

    public Service(ConnectionFactory mainFactory, ConnectionFactory subFactory, string rpcQueue, int prefetchCount, int maxPriority, ILogger logger)
    {
        _logger = logger;
        _mainConnection = mainFactory.CreateConnectionLoop(logger);
        _subConnection = subFactory.CreateConnectionLoop(logger);
        _mainChannel = _mainConnection.CreateModel();
        _subChannel = _subConnection.CreateModel();

        _rpcQueue = rpcQueue;
        _prefetchCount = prefetchCount;
        _maxPriority = maxPriority;
    }

    public void Open()
    {
        var args = new Dictionary<string, object>();
        if (_maxPriority > 0)
            args.Add("x-max-priority", _maxPriority);

        _mainChannel.QueueDeclare(_rpcQueue, false, false, true, args);
        var consumer = new AsyncEventingBasicConsumer(_mainChannel);
        _mainChannel.BasicQos(0, (ushort)_prefetchCount, false);
        _consumerTag = _mainChannel.BasicConsume(_rpcQueue, false, consumer);
        consumer.Received += (_, e) => OnReceivedAsync(new EventArgsT<CallSession>(new CallSession(_mainChannel, _subChannel, e, _logger)));
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        if (_consumerTag != null)
            _mainChannel.TryBasicCancel(_consumerTag, _logger);

        _subChannel.TryClose(_logger);
        _mainChannel.TryClose(_logger);

        _subConnection.TryClose(_logger);
        _mainConnection.TryClose(_logger);
    }

    private Task OnReceivedAsync(EventArgsT<CallSession> e)
    {
        return ReceivedAsync.InvokeAsync(this, e);
    }
}