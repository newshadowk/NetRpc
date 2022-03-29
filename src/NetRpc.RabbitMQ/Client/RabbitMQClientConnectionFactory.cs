using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Proxy.RabbitMQ;
using RabbitMQ.Client;

namespace NetRpc.RabbitMQ;

public class RabbitMQClientConnectionFactory : IClientConnectionFactory
{
    private readonly ILogger _logger;
    private readonly IConnection _mainConnection;
    private readonly IConnection _subConnection;
    private readonly IModel _mainChannel;
    private readonly IModel _subChannel;
    private readonly MQOptions _options;
    private readonly object _lockObj = new();
    private volatile bool _disposed;

    public RabbitMQClientConnectionFactory(IOptions<RabbitMQClientOptions> options, ILoggerFactory factory)
    {
        _logger = factory.CreateLogger("NetRpc");
        _options = options.Value;

        //main
        _mainConnection = _options.CreateConnectionFactory().CreateConnectionLoop(_logger);
        _mainChannel = _mainConnection.CreateModel();
        _mainChannel.QueueDeclare(_options.RpcQueue, false, false, false,
            (_options.MaxPriority > 0 ? new Dictionary<string, object> { { "x-max-priority", _options.MaxPriority } } : null)!);

        //sub
        _subConnection = _options.CreateConnectionFactory_TopologyRecovery_Disabled().CreateConnectionLoop(_logger);
        _subChannel = _subConnection.CreateModel();
    }

    public IClientConnection Create(bool isRetry)
    {
        lock (_lockObj)
            return new RabbitMQClientConnection(_mainConnection, _mainChannel, _subChannel, _options, _logger);
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