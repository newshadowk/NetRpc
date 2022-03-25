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
    private readonly ILogger _logger;
    private readonly IConnection _connect;
    private volatile IModel? _mainChannel;
    private volatile bool _disposed;
    private readonly int _maxPriority;
    private volatile string? _consumerTag;

    public ServiceInner(IConnection connect, string rpcQueue, int prefetchCount, int maxPriority, ILogger logger)
    {
        _connect = connect;
        _rpcQueue = rpcQueue;
        _prefetchCount = prefetchCount;
        _logger = logger;
        _maxPriority = maxPriority;
    }

    public void CreateChannel()
    {
        _mainChannel = _connect.CreateModel();
        var args = new Dictionary<string, object>();
        if (_maxPriority > 0)
            args.Add("x-max-priority", _maxPriority);
        _mainChannel.QueueDeclare(_rpcQueue, false, false, false, args);
        var consumer = new AsyncEventingBasicConsumer(_mainChannel);
        _mainChannel.BasicQos(0, (ushort)_prefetchCount, false);
        _consumerTag = _mainChannel.BasicConsume(_rpcQueue, false, consumer);
        consumer.Received += (_, e) => OnReceivedAsync(new EventArgsT<CallSession>(new CallSession(_connect, _mainChannel, e, _logger)));
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        try
        {
            if (_consumerTag != null)
                _mainChannel?.BasicCancel(_consumerTag);
            _mainChannel?.Close();
            _mainChannel?.Dispose();
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