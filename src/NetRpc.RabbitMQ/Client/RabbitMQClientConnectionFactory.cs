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
    private readonly IConnection _cmdConn;
    private readonly IConnection _tmpConn;
    private readonly IModel _cmdChannel;
    private readonly IModel _tmpChannel;
    private readonly MQOptions _options;
    private readonly object _lockObj = new();
    private volatile bool _disposed;

    public RabbitMQClientConnectionFactory(IOptions<RabbitMQClientOptions> options, ILoggerFactory factory)
    {
        _logger = factory.CreateLogger("NetRpc");
        _options = options.Value;

        //cmd
        _cmdConn = _options.CreateConnectionFactory().CreateConnectionLoop(_logger);
        _cmdChannel = _cmdConn.CreateModel();
        _cmdChannel.QueueDeclare(_options.RpcQueue, false, false, false,
            (_options.MaxPriority > 0 ? new Dictionary<string, object> { { "x-max-priority", _options.MaxPriority } } : null)!);

        //tmp
        _tmpConn = _options.CreateConnectionFactory_TopologyRecovery_Disabled().CreateConnectionLoop(_logger);
        _tmpChannel = _tmpConn.CreateModel();
    }

    public IClientConnection Create(bool isRetry)
    {
        lock (_lockObj)
            return new RabbitMQClientConnection(_cmdConn, _cmdChannel, _tmpChannel, _options, _logger);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        try
        {
            _cmdChannel.Close();
            _tmpChannel.Close();
            _cmdConn.Close();
            _tmpConn.Close();
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, null);
        }
    }
}