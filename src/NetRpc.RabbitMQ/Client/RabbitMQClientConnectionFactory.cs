using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Proxy.RabbitMQ;

namespace NetRpc.RabbitMQ;

public sealed class RabbitMQClientConnectionFactory : IClientConnectionFactory
{
    private readonly ClientConnection _conn;
    private volatile bool _disposed;

    public RabbitMQClientConnectionFactory(ClientConnection conn)
    {
        _conn = conn;
    }

    public RabbitMQClientConnectionFactory(IOptions<MQClientOptions> options, ILoggerFactory factory)
    {
        _conn = new ClientConnection(options.Value, factory);
    }

    public IClientConnection Create(bool isRetry)
    {
        return new RabbitMQClientConnection(_conn);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _conn.Dispose();
    }
}