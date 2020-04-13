using Microsoft.Extensions.Logging;

namespace NetRpc.RabbitMQ
{
    public sealed class RabbitMQClientConnectionFactoryOptions
    {
        public RabbitMQClientConnectionFactory Factory { get; }

        public RabbitMQClientConnectionFactoryOptions(MQOptions options, ILoggerFactory loggerFactory)
        {
            Factory = new RabbitMQClientConnectionFactory(
                new SimpleOptions<RabbitMQClientOptions>(new RabbitMQClientOptions(options)), loggerFactory);
        }
    }
}