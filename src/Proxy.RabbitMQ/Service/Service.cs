using System;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace RabbitMQ.Base
{
    public sealed class Service : IDisposable
    {
        public event EventHandler<EventArgsT<CallSession>> Received;
        public event EventHandler<ShutdownEventArgs> ConnectionShutdown;
        public event EventHandler<EventArgs> RecoverySucceeded;
        private IConnection _connection;
        private readonly ConnectionFactory _factory;
        private readonly string _rpcQueue;
        private readonly int _prefetchCount;
        private readonly ILogger _logger;
        private volatile bool _disposed;

        public ServiceInner Inner { get; private set; }

        public Service(ConnectionFactory factory, string rpcQueue, int prefetchCount, ILogger logger)
        {
            _factory = factory;
            _rpcQueue = rpcQueue;
            _prefetchCount = prefetchCount;
            _logger = logger;
        }

        public void Open()
        {
            _connection = _factory.CreateConnection();
            _connection.RecoverySucceeded += ConnectionRecoverySucceeded;
            _connection.ConnectionShutdown += ConnectionConnectionShutdown;
            ResetService();
        }

        private void ConnectionConnectionShutdown(object sender, ShutdownEventArgs e)
        {
            OnConnectionShutdown(e);
        }

        private void ConnectionRecoverySucceeded(object sender, EventArgs e)
        {
            try
            {
                ResetService();
                OnRecoverySucceeded();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, null);
            }
        }

        private void ResetService()
        {
            Inner?.Dispose();
            Inner = new ServiceInner(_connection, _rpcQueue, _prefetchCount, _logger);
            Inner.CreateChannel();
            Inner.Received += InnerReceived;
        }

        private void InnerReceived(object sender, EventArgsT<CallSession> e)
        {
            OnReceived(e);
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

        private void OnRecoverySucceeded()
        {
            RecoverySucceeded?.Invoke(this, EventArgs.Empty);
        }

        private void OnReceived(EventArgsT<CallSession> e)
        {
            Received?.Invoke(this, e);
        }
    }
}