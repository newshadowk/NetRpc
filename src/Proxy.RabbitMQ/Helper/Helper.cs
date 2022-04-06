using System;
using System.Threading;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace Proxy.RabbitMQ;

public static class Helper
{
    public static IConnection CreateConnectionLoop(this ConnectionFactory factory, ILogger logger)
    {
        while (true)
        {
            try {
                logger.LogInformation("Create Mq connection...");
                var conn = factory.CreateConnection();
                logger.LogInformation("Create Mq connection...ok.");
                return conn;
            } catch (BrokerUnreachableException)
            {
                logger.LogInformation("Create Mq connection...failed. retry after 3s.");
                Thread.Sleep(3000);
            }
        }
    }

    public static void TryClose(this IModel model, ILogger log)
    {
        try
        {
            model.Close();
        }
        catch (Exception e)
        {
            log.LogWarning(e, null);
        }
    }

    public static void TryClose(this IConnection connection, ILogger log)
    {
        try
        {
            connection.Close();
        }
        catch (Exception e)
        {
            log.LogWarning(e, null);
        }
    }

    public static void TryBasicCancel(this IModel model, string? consumerTag, ILogger log)
    {
        try
        {
            if (consumerTag != null && model.IsOpen)
                model.BasicCancel(consumerTag);
        }
        catch (Exception e)
        {
            log.LogWarning(e, null);
        }
    }

    public static void TryBasicAck(this IModel model, ulong deliveryTag, bool multiple, ILogger log)
    {
        try
        {
            model.BasicAck(deliveryTag, multiple);
        }
        catch (Exception e)
        {
            log.LogWarning(e, null);
        }
    }
}