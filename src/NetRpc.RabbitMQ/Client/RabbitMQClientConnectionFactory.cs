using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace NetRpc.RabbitMQ
{
    public class RabbitMQClientConnectionFactory : IClientConnectionFactory
    {
        private readonly ILogger _logger;
        private IConnection _connection;
        private MQOptions _options;
        private readonly object _lockObj = new object();

        public RabbitMQClientConnectionFactory(IOptionsMonitor<RabbitMQClientOptions> options, ILoggerFactory factory)
        {
            _logger = factory.CreateLogger("NetRpc");
            _options = options.CurrentValue;
            _connection = _options.CreateConnectionFactory().CreateConnection();
            options.OnChange(i =>
            {
                lock (_lockObj)
                {
                    _options = i;
                    _connection = _options.CreateConnectionFactory().CreateConnection();
                }
            });
        }

        public void Dispose()
        {
            _connection?.Close();
            _connection?.Dispose();
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
                return new RabbitMQClientConnection(_connection, _options.RpcQueue, _logger);
        }
    }
}