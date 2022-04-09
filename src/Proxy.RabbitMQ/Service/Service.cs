using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Proxy.RabbitMQ;

public sealed class Service : IDisposable
{
    private readonly MQConnection _conn;
    public event AsyncEventHandler<EventArgsT<CallSession>>? ReceivedAsync;
    private readonly ILogger _logger;
    private volatile bool _disposed;
    private volatile bool _stopped;
    private volatile string? _consumerTag;
    private readonly object _lockDispose = new ();

    public Service(MQOptions options, ILoggerFactory factory)
    {
        _conn = new MQConnection(options, false, factory);
        _logger = _conn.Logger;
    }

    public void Open()
    {
        var args = new Dictionary<string, object>();
        if (_conn.Options.MaxPriority > 0)
            args.Add("x-max-priority", _conn.Options.MaxPriority);

        _conn.MainChannel.QueueDeclare(_conn.Options.RpcQueue, false, false, true, args);
        var consumer = new AsyncEventingBasicConsumer(_conn.MainChannel);
        _conn.MainChannel.BasicQos(0, (ushort)_conn.Options.PrefetchCount, false);
        _consumerTag = _conn.MainChannel.BasicConsume(_conn.Options.RpcQueue, true, consumer);
        consumer.Received += (_, e) =>
        {
            if (!_conn.Checker.Check(e.BasicProperties.ReplyTo))
            {
                _logger.LogWarning($"request ignore, {e.BasicProperties.ReplyTo} is not found.");
                return Task.CompletedTask;
            }
            return OnReceivedAsync(new EventArgsT<CallSession>(new CallSession(_conn.SubConnection, _conn.SubWatcher, e, _logger)));
        };
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