using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Proxy.RabbitMQ;
using RabbitMQ.Client;

namespace NetRpc.RabbitMQ;

public class RabbitMQClientConnectionFactory : IClientConnectionFactory
{
    private readonly ILogger _logger;
    private readonly IAutorecoveringConnection _mainConnection;
    private readonly IConnection _subConnection;
    private readonly IModel _mainChannel;
    private readonly IModel _subChannel;
    private readonly MQOptions _options;
    private readonly MainWatcher _mainWatcher;
    private readonly SubWatcher _subWatcher;
    private volatile bool _disposed;

    public RabbitMQClientConnectionFactory(IOptions<RabbitMQClientOptions> options, ILoggerFactory factory)
    {
        _logger = factory.CreateLogger("NetRpc");
        _options = options.Value;

        //main
        _mainConnection = (IAutorecoveringConnection)_options.CreateConnectionFactory().CreateConnectionLoop(_logger);
        _mainConnection.ConnectionShutdown += (_, e) => _logger.LogInformation($"Client ConnectionShutdown, {e.ReplyCode}, {e.ReplyText}");
        _mainConnection.ConnectionRecoveryError += (_, e) => _logger.LogInformation($"Client ConnectionRecoveryError, {e.Exception.Message}");
        _mainConnection.RecoverySucceeded += (_, _) => _logger.LogInformation("Client RecoverySucceeded");

        _mainChannel = _mainConnection.CreateModel();

        //sub
        _subConnection = _options.CreateConnectionFactory_TopologyRecovery_Disabled().CreateConnectionLoop(_logger);
        _subChannel = _subConnection.CreateModel();

        _subWatcher = new SubWatcher(new ExclusiveChecker(_subConnection));
        _mainWatcher = new(_mainConnection, options.Value.RpcQueue);
    }

    public IClientConnection Create(bool isRetry)
    {
        return new RabbitMQClientConnection(_mainConnection, _subConnection, _mainChannel, _mainWatcher, _subWatcher, _options, _logger);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        _subChannel.TryClose(_logger);
        _mainChannel.TryClose(_logger);
        _subConnection.TryClose(_logger);
        _mainConnection.TryClose(_logger);
    }
}