using Microsoft.Extensions.Options;

namespace NetRpc.RabbitMQ
{
    public class RabbitMQClientConnectionFactory : IConnectionFactory
    {
        private global::RabbitMQ.Client.IConnection _connection;
        private MQOptions _options;
        private readonly object _lockObj = new object();

        public RabbitMQClientConnectionFactory(IOptionsMonitor<RabbitMQClientOptions> options)
        {
            _options = options.CurrentValue.Value;
            _connection = _options.CreateConnectionFactory().CreateConnection();
            options.OnChange(i =>
            {
                lock (_lockObj)
                {
                    _options = i.Value;
                    _connection = _options.CreateConnectionFactory().CreateConnection();
                }
            });
        }

        public void Dispose()
        {
            _connection?.Close();
            _connection?.Dispose();
        }

        public IConnection Create()
        {
            lock (_lockObj)
                return new ClientConnection(_connection, _options.RpcQueue);
        }
    }
}