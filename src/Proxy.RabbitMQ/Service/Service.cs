using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Proxy.RabbitMQ;

public sealed class Service : IDisposable
{
    public event AsyncEventHandler<EventArgsT<CallSession>>? ReceivedAsync;
    private readonly IConnection _mainConnection;
    private readonly IModel _mainChannel;
    private readonly string _rpcQueue;
    private readonly int _prefetchCount;
    private readonly int _maxPriority;
    private readonly ILogger _logger;
    private volatile bool _disposed;
    private volatile string? _consumerTag;

    public Service(ConnectionFactory mainFactory, string rpcQueue, int prefetchCount, int maxPriority, ILogger logger)
    {
        _logger = logger;
        _mainConnection = mainFactory.CreateConnectionLoop(logger);
        _mainChannel = _mainConnection.CreateModel();
        _mainConnection.ConnectionShutdown += OnConnectionShutdown;

        _rpcQueue = rpcQueue;
        _prefetchCount = prefetchCount;
        _maxPriority = maxPriority;
    }

    private void OnConnectionShutdown(object? sender, ShutdownEventArgs e)
    {
        _logger.LogInformation($"OnConnectionShutdown, {e.ReplyCode}, {e.ReplyText}, {e.ClassId}, {e.MethodId}");
        Environment.Exit(0);
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
        consumer.Received += (_, e) => OnReceivedAsync(new EventArgsT<CallSession>(new 
            CallSession(_mainChannel, e, _logger)));
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        if (_consumerTag != null)
            _mainChannel.TryBasicCancel(_consumerTag, _logger);

        _mainChannel.TryClose(_logger);
        _mainConnection.TryClose(_logger);
    }

    private Task OnReceivedAsync(EventArgsT<CallSession> e)
    {
        return ReceivedAsync.InvokeAsync(this, e);
    }
}