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
    private readonly IAutorecoveringConnection _mainConnection;
    private readonly IModel _mainChannel;
    private readonly string _rpcQueue;
    private readonly int _prefetchCount;
    private readonly int _maxPriority;
    private readonly ILogger _logger;
    private volatile bool _disposed;
    private volatile string? _consumerTag;
    private readonly SubWatcher _subWatcher;
    private readonly IModel _subChannel;

    public Service(ConnectionFactory mainFactory, ConnectionFactory subFactory, string rpcQueue, int prefetchCount, int maxPriority, ILogger logger)
    {
        _logger = logger;
        _mainConnection = (IAutorecoveringConnection)mainFactory.CreateConnectionLoop(logger);
        _mainConnection.ConnectionShutdown += (_, e) => _logger.LogInformation(LogStr($"ConnectionShutdown, {e.ReplyCode}, {e.ReplyText}"));
        _mainConnection.ConnectionRecoveryError += (_, e) => _logger.LogInformation(LogStr($"ConnectionRecoveryError, {e.Exception.Message}"));
        _mainConnection.RecoverySucceeded += (_, _) => _logger.LogInformation(LogStr("RecoverySucceeded"));

        _mainChannel = _mainConnection.CreateModel();

        var subConnection = subFactory.CreateConnectionLoop(logger);
        _subChannel = subConnection.CreateModel();
        _subWatcher = new SubWatcher(subConnection, logger);
        _rpcQueue = rpcQueue;
        _prefetchCount = prefetchCount;
        _maxPriority = maxPriority;
    }

    private static string LogStr(string s)
    {
        return $"====================\r\n{s}\r\n====================";
    }

    public void Open()
    {
        var args = new Dictionary<string, object>();
        if (_maxPriority > 0)
            args.Add("x-max-priority", _maxPriority);

        _mainChannel.QueueDeclare(_rpcQueue, false, false, true, args);
        var consumer = new AsyncEventingBasicConsumer(_mainChannel);
        _mainChannel.BasicQos(0, (ushort)_prefetchCount, false);
        _consumerTag = _mainChannel.BasicConsume(_rpcQueue, true, consumer);
        consumer.Received += (_, e) => OnReceivedAsync(new EventArgsT<CallSession>(new CallSession(_subChannel, _subWatcher, e, _logger)));
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        _mainChannel.TryBasicCancel(_consumerTag, _logger);
        _mainChannel.TryClose(_logger);
        _mainConnection.TryClose(_logger);
    }

    private Task OnReceivedAsync(EventArgsT<CallSession> e)
    {
         return ReceivedAsync.InvokeAsync(this, e);
    }
}