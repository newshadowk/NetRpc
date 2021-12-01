using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace RabbitMQ.Base;

public sealed class Service : IDisposable
{
    public event AsyncEventHandler<EventArgsT<CallSession>>? ReceivedAsync;
    public event EventHandler<ShutdownEventArgs>? ConnectionShutdown;
    private IConnection? _connection;
    private readonly ConnectionFactory _factory;
    private readonly string _rpcQueue;
    private readonly int _prefetchCount;
    private readonly bool _durable;
    private readonly bool _autoDelete;
    private readonly int _retryCount;
    private readonly int _maxPriority;
    private readonly ILogger _logger;
    private volatile bool _disposed;
    private readonly object _lockObj = new();

    public ServiceInner? Inner { get; private set; }

    public Service(ConnectionFactory factory, string rpcQueue, int prefetchCount, int maxPriority, bool durable, bool autoDelete,int retryCount, ILogger logger)
    {
        _factory = factory;
        _rpcQueue = rpcQueue;
        _prefetchCount = prefetchCount;
        _durable = durable;
        _autoDelete = autoDelete;
        _logger = logger;
        _retryCount = retryCount;
        _maxPriority = maxPriority;
    }

    public void Open()
    {
        TryConnect();
        //_connection = _factory.CreateConnection();
        //_connection.ConnectionShutdown += ConnectionConnectionShutdown;
        Inner = new ServiceInner(_connection, _rpcQueue, _prefetchCount, _maxPriority, _durable, _autoDelete, _logger);
        Inner.CreateChannel();
        Inner.ReceivedAsync += (_, e) => OnReceivedAsync(e);
    }

    private bool IsConnected => _connection != null && _connection.IsOpen && !_disposed;

    private bool TryConnect()
    {
        _logger.LogInformation("RabbitMQ Client is trying to connect");

        lock (_lockObj)
        {
            var policy = Policy.Handle<SocketException>()
                .Or<BrokerUnreachableException>()
                .WaitAndRetry(_retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                    {
                        _logger.LogWarning(ex, "RabbitMQ Client could not connect after {TimeOut}s ({ExceptionMessage})", $"{time.TotalSeconds:n1}", ex.Message);
                    }
                );

            policy.Execute(() =>
            {
                _connection = _factory.CreateConnection();
            });

            if (IsConnected)
            {
                _connection.ConnectionShutdown += OnConnectionShutdown;
                _connection.ConnectionShutdown += ConnectionConnectionShutdown;
                _connection.CallbackException += OnCallbackException;
                _connection.ConnectionBlocked += OnConnectionBlocked;

                _logger.LogInformation("RabbitMQ Client acquired a persistent connection to '{HostName}' and is subscribed to failure events", _connection.Endpoint.HostName);

                return true;
            }
            else
            {
                _logger.LogCritical("FATAL ERROR: RabbitMQ connections could not be created and opened");

                return false;
            }
        }
    }

    private void OnConnectionBlocked(object sender, ConnectionBlockedEventArgs e)
    {
        if (_disposed) return;

        _logger.LogWarning("A RabbitMQ connection is shutdown. Trying to re-connect...");

        TryConnect();
    }

    void OnCallbackException(object sender, CallbackExceptionEventArgs e)
    {
        if (_disposed) return;

        _logger.LogWarning("A RabbitMQ connection throw exception. Trying to re-connect...");

        TryConnect();
    }

    void OnConnectionShutdown(object sender, ShutdownEventArgs reason)
    {
        if (_disposed) return;

        _logger.LogWarning("A RabbitMQ connection is on shutdown. Trying to re-connect...");

        TryConnect();
    }

    private void ConnectionConnectionShutdown(object? sender, ShutdownEventArgs e)
    {
        OnConnectionShutdown(e);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        try
        {
            _connection?.Close();
            _connection?.Dispose();
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, null);
        }
    }

    private void OnConnectionShutdown(ShutdownEventArgs e)
    {
        ConnectionShutdown?.Invoke(this, e);
    }

    private Task OnReceivedAsync(EventArgsT<CallSession> e)
    {
        return ReceivedAsync.InvokeAsync(this, e);
    }
}