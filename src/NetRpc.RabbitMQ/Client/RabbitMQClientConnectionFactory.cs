using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace NetRpc.RabbitMQ
{
    public class RabbitMQClientConnectionFactory : IClientConnectionFactory
    {
        private readonly ILogger _logger;
        private readonly IConnection _connection;
        private readonly MQOptions _options;
        private readonly object _lockObj = new object();
        private volatile bool _disposed;

        public RabbitMQClientConnectionFactory(IOptions<RabbitMQClientOptions> options, ILoggerFactory factory)
        {
            _logger = factory.CreateLogger("NetRpc");
            _options = options.Value;
            _connection = _options.CreateConnectionFactory().CreateConnection();
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

#if NETSTANDARD2_1 || NETCOREAPP3_1
        public System.Threading.Tasks.ValueTask DisposeAsync()
        {
            Dispose();
            return new System.Threading.Tasks.ValueTask();
        }
#endif

        public IClientConnection Create()
        {
            lock (_lockObj)
                return new RabbitMQClientConnection(_connection, _options, _logger);
        }
    }
}