using Proxy.RabbitMQ;

namespace NetRpc.RabbitMQ;

public sealed class RabbitMQClientConnectionFactory : IClientConnectionFactory
{
    private readonly ClientConnection _conn;

    public RabbitMQClientConnectionFactory(ClientConnectionCache cache)
    {
        _conn = cache.GetClient();
    }

    public RabbitMQClientConnectionFactory(ClientConnection conn)
    {
        _conn = conn;
    }

    public IClientConnection Create(bool isRetry)
    {
        return new RabbitMQClientConnection(_conn);
    }

    public void Dispose()
    {
    }
}