using System;
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
        private readonly string _rpcQueueName;
        private readonly int _prefetchCount;
        public ServiceInner Inner { get; private set; }
        public Service(ConnectionFactory factory, string rpcQueueName, int prefetchCount)
        {
            _factory = factory;
            _rpcQueueName = rpcQueueName;
            _prefetchCount = prefetchCount;
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
            catch
            {
            }
        }

        private void ResetService()
        {
            Inner?.Dispose();
            Inner = new ServiceInner(_connection, _rpcQueueName, _prefetchCount);
            Inner.CreateChannel();
            Inner.Received += InnerReceived;
        }

        private void InnerReceived(object sender, EventArgsT<CallSession> e)
        {
            OnReceived(e);
        }

        public void Dispose()
        {
            _connection.Close();
            _connection.Dispose();
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
