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
}