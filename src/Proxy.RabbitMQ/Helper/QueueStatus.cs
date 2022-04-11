using System;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Proxy.RabbitMQ;

//public sealed class QueueStatus
//{
//    private readonly MQOptions _options;
//    private readonly ILogger _logger;
//    private readonly IAutorecoveringConnection _mainConnection;
//    private readonly IModel _mainChannel;

//    public QueueStatus(MQOptions options, ILoggerFactory factory)
//    {
//        _options = options;
//        _logger = factory.CreateLogger("NetRpc");
//        _mainConnection = (IAutorecoveringConnection)options.CreateConnectionFactory().CreateConnectionLoop(_logger);
//        _mainChannel = _mainConnection.CreateModel();
//    }

//    public int GetMainQueueMsgCount()
//    {
//        try
//        {

//        }
//        catch (Exception e)
//        {
//            Console.WriteLine(e);
//            throw;
//        }
//        var ok = _mainChannel.QueueDeclarePassive(_options.RpcQueue);
//    }
//}