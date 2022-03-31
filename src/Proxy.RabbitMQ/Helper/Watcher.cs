using System;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.Extensions.Logging.Abstractions;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Proxy.RabbitMQ;

public sealed class ReceiveWatcher : IDisposable
{
    private const int CheckIntervalSecs = 2;
    private const int HeartBeatIntervalTimeoutSecs = 7;
    private volatile bool _disposed;
    private volatile IModel _channel;
    private readonly Timer _t = new(CheckIntervalSecs * 1000);
    private volatile string? _consumerTag;
    private DateTimeOffset _lastTime = DateTimeOffset.Now;

    public event EventHandler? Disconnected;

    public ReceiveWatcher(IModel subChannel)
    {
        _channel = subChannel;
    }

    private void Elapsed(object? sender, ElapsedEventArgs e)
    {
        if ((DateTimeOffset.Now - _lastTime).TotalSeconds >= HeartBeatIntervalTimeoutSecs)
        {
            Dispose();
            OnDisconnected();
        }
    }

    public string Start()
    {
        var name = _channel.QueueDeclare().QueueName;
        var c = new AsyncEventingBasicConsumer(_channel);
        c.Received += (_, _) =>
        {
            _lastTime = DateTimeOffset.Now;
            return Task.CompletedTask;
        };
        _consumerTag = _channel.BasicConsume(name, true, c);
        _t.Elapsed += Elapsed;       
        _t.Start();
        return name;
    }

    private void OnDisconnected()
    {
        Disconnected?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        _t.Dispose();
        if (_consumerTag != null)
            _channel.TryBasicCancel(_consumerTag, NullLogger.Instance);
    }
}

public sealed class SendWatcher : IDisposable
{
    private const int HeartBeatIntervalSecs = 2;
    private readonly IModel _channel;
    private readonly string _queue;
    private readonly string _cid = Guid.NewGuid().ToString("N");
    private readonly Timer _t = new(HeartBeatIntervalSecs * 1000);
    private readonly IBasicProperties _p;
    public event EventHandler? Disconnected;

    public SendWatcher(IModel subChannel, string queue)
    {
        _channel = subChannel;
        _queue = queue;

        _channel.BasicReturn += BasicReturn;
        _p = _channel.CreateBasicProperties();
    }

    private void BasicReturn(object? sender, BasicReturnEventArgs e)
    {
        if (e.BasicProperties.CorrelationId == _cid)
        {
            Dispose();
            OnDisconnected();
        }
    }

    public void Start()
    {
        _t.Elapsed += Elapsed;
        _t.Start();
    }

    private void Elapsed(object? sender, ElapsedEventArgs e)
    {
        _channel.BasicPublish("", _queue, true, _p);
    }

    private void OnDisconnected()
    {
        Disconnected?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        _t.Dispose();
    }
}