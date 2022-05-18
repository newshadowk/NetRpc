using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Proxy.RabbitMQ;

public sealed class Service : IDisposable
{
    private readonly ServiceConnection _conn;
    public event AsyncEventHandler<EventArgsT<CallSession>>? ReceivedAsync;
    private readonly ILogger _logger;
    private volatile bool _disposed;
    private volatile bool _stopped;
    private volatile string? _consumerTag;
    private readonly object _lockDispose = new ();

    public Service(MQServiceOptions options, ILoggerFactory factory)
    {
        _conn = new ServiceConnection(options, factory);
        _conn.Connection.RecoverySucceeded += RecoverySucceeded;
        _logger = _conn.Logger;
    }

    public void Open()
    {
        QueueDeclareRpc();
        RegConsumer();
    }

    private void QueueDeclareRpc()
    {
        var args = new Dictionary<string, object>();
        if (_conn.Options.MaxPriority > 0)
            args.Add("x-max-priority", _conn.Options.MaxPriority);

        _conn.MainChannel.QueueDeclare(_conn.Options.RpcQueue, false, false, false, args);
    }

    private void RegConsumer()
    {
        var consumer = new AsyncEventingBasicConsumer(_conn.MainChannel);
        consumer.Received += ConsumerReceived;
        consumer.ConsumerCancelled += ConsumerConsumerCancelled;
        _consumerTag = _conn.MainChannel.BasicConsume(_conn.Options.RpcQueue, false, consumer);
    }

    private void Recovery()
    {
        _logger.LogWarning("Recovery start.");
        _logger.LogWarning($"Connection.IsOpen:{_conn.Connection.IsOpen}");

        if (!_conn.Connection.IsOpen)
        {
            _logger.LogWarning("Recovery cancelled, wait connection recovery...");
            return;
        }

        _logger.LogWarning($"Checking {_conn.Options.RpcQueue} exist.");

        var exist = ChannelChecker.Check(_conn.Connection, _conn.Options.RpcQueue);

        _logger.LogWarning($"{_conn.Options.RpcQueue} exist: {exist}");
        _logger.LogWarning($"QueueDeclare: {_conn.Options.RpcQueue}");

        try
        {
            QueueDeclareRpc();

            _logger.LogWarning("RegConsumer again.");

            RegConsumer();
            _logger.LogWarning("Recovery ok.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Recovery failed.");
        }
    }

    private void RecoverySucceeded(object? sender, EventArgs e)
    {
        Recovery();
    }

    private Task ConsumerConsumerCancelled(object sender, ConsumerEventArgs e)
    {
        var consumer = (AsyncEventingBasicConsumer)sender;
        consumer.Received -= ConsumerReceived;
        consumer.ConsumerCancelled -= ConsumerConsumerCancelled;

        Recovery();

        return Task.CompletedTask;
    }

    private Task ConsumerReceived(object sender, BasicDeliverEventArgs e)
    {
        if (!_conn.Checker.Check(e.BasicProperties.ReplyTo))
        {
            _logger.LogWarning($"request ignore, {e.BasicProperties.ReplyTo} is not found.");
            _conn.MainChannel.TryBasicAck(e.DeliveryTag, _logger);
            return Task.CompletedTask;
        }
        return OnReceivedAsync(new EventArgsT<CallSession>(new CallSession(_conn.Connection, _conn.SubWatcher, _conn.MainChannel, e, _logger)));
    }

    public void Stop()
    {
        lock (_lockDispose)
        {
            if (_stopped)
                return;
            _stopped = true;

            _logger.LogInformation("Service Stop start.");
            _conn.MainChannel.TryBasicCancel(_consumerTag, _logger);
            _conn.MainChannel.TryClose(_logger);
            _logger.LogInformation("Service Stop ok.");
        }
    }

    public void Dispose()
    {
        lock (_lockDispose)
        {
            if (_disposed)
                return;
            _disposed = true;
            _logger.LogInformation("Service Dispose start.");

            Stop();

            _conn.Dispose();
            _logger.LogInformation("Service Dispose ok.");
        }
    }

    private Task OnReceivedAsync(EventArgsT<CallSession> e)
    {
         return ReceivedAsync.InvokeAsync(this, e);
    }
}