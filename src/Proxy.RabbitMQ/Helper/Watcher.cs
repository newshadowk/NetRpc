using System;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Proxy.RabbitMQ;

public sealed class QueueWatcher : IDisposable
{
    private const int HeartBeatIntervalSecs = 2;
    private const int HeartBeatIntervalTimeoutSecs = 7;
    private readonly IModel _channel;
    private readonly ILogger _logger;
    private volatile string? _sendQueue;
    private volatile bool _disposed;
    private readonly object _lock_disposed = new();
    private volatile string? _consumerTag;
    private readonly Timer _t = new(HeartBeatIntervalSecs * 1000);
    private DateTimeOffset _lastTime = DateTimeOffset.Now;
    private readonly byte[] _buffer =  { 0xFF };

    public event EventHandler? Disconnected;

    public QueueWatcher(IModel channel, ILogger logger)
    {
        _t.Elapsed += Elapsed;       
        _channel = channel;
        _logger = logger;
    }

    public string? StartListen()
    {
        string name;

        try
        {
            name = _channel.QueueDeclare().QueueName;
            var c = new AsyncEventingBasicConsumer(_channel);
            c.Received += (_, _) =>
            {
                _lastTime = DateTimeOffset.Now;
                return Task.CompletedTask;
            };
            _consumerTag = _channel.BasicConsume(name, true, c);
        }
        catch
        {
            _logger.LogWarning("StartListen failed.");
            return null;
        }

        _t.Start();

        return name;
    }

    public void StartSend(string sendQueue)
    {
        lock (_lock_disposed)
        {
            if (_disposed)
                return;

            _t.Start();
        }
    
        _sendQueue = sendQueue;
    }

    private void Elapsed(object? sender, ElapsedEventArgs e)
    {
        var now = DateTimeOffset.Now;
        if ((DateTimeOffset.Now - _lastTime).TotalSeconds >= HeartBeatIntervalTimeoutSecs)
        {
            Dispose();
            _logger.LogWarning($"watcher disconnected, now:{now} - lastTime:{_lastTime} = {(DateTimeOffset.Now - _lastTime).TotalSeconds} >= {HeartBeatIntervalTimeoutSecs}");
            OnDisconnected();
            return;
        }

        if (_sendQueue != null)
        {
            try
            {
                _channel.BasicPublish("", _sendQueue, null, _buffer);
            }
            catch
            {
            }
        }
    }

    private void OnDisconnected()
    {
        Disconnected?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        lock (_lock_disposed)
        {
            if (_disposed)
                return;
            _disposed = true;

            _t.Dispose();
        }

        if (_consumerTag != null)
            _channel.TryBasicCancel(_consumerTag, NullLogger.Instance);
    }
}