using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RabbitMQ.Base
{
    public sealed class Service : IDisposable
    {
        public event AsyncEventHandler<EventArgsT<CallSession>>? ReceivedAsync;
        public event EventHandler<ShutdownEventArgs>? ConnectionShutdown;
        private IConnection? _connection;
        private readonly ConnectionFactory _factory;
        private readonly string _rpcQueue;
        private readonly int _prefetchCount;
        private readonly bool _durable;
        private readonly int _maxPriority;
        private readonly bool _autoDelete;
        private readonly ILogger _logger;
        private volatile bool _disposed;

        public ServiceInner? Inner { get; private set; }

        public Service(ConnectionFactory factory, string rpcQueue, int prefetchCount, int maxPriority, bool durable, bool autoDelete, ILogger logger)
        {
            _factory = factory;
            _rpcQueue = rpcQueue;
            _prefetchCount = prefetchCount;
            _durable = durable;
            _autoDelete = autoDelete;
            _logger = logger;
            _maxPriority = maxPriority;
        }

        public void Open()
        {
            _connection = _factory.CreateConnection();
            _connection.ConnectionShutdown += ConnectionConnectionShutdown;
            Inner = new ServiceInner(_connection, _rpcQueue, _prefetchCount, _maxPriority, _durable, _autoDelete, _logger);
            Inner.CreateChannel();
            Inner.ReceivedAsync += (_, e) => OnReceivedAsync(e);
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
}