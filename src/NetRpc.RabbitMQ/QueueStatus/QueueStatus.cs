using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Proxy.RabbitMQ;
using RabbitMQ.Client;

namespace NetRpc.RabbitMQ;

public sealed class QueueStatus : IDisposable
{
    private readonly MQOptions _options;
    private readonly IAutorecoveringConnection _mainConnection;
    private readonly IModel _mainChannel;

    public QueueStatus(MQOptions options, ILoggerFactory factory)
    {
        _options = options;
        var logger = factory.CreateLogger("NetRpc");
        _mainConnection = (IAutorecoveringConnection)options.CreateMainConnectionFactory().CreateConnectionLoop(logger);
        _mainChannel = _mainConnection.CreateModel();
    }

    public QueueStatus(IOptions<QueueStatusOptions> options, ILoggerFactory factory) : this(options.Value, factory)
    {
    }

    public int GetMainQueueMsgCount()
    {
        try
        {
            var ok = _mainChannel.QueueDeclarePassive(_options.RpcQueue);
            return (int)ok.MessageCount;
        }
        catch
        {
            return -1;
        }
    }

    public void Dispose()
    {
        _mainChannel.Dispose();
        _mainConnection.Dispose();
    }
}