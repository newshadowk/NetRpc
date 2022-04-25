using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace Proxy.RabbitMQ;

public static class Helper
{
    private static void PopulateFromUrl(MQOptions options)
    {
        if (options.Url == null)
            return;
        //amqp://user:pass@host:port/virtualHost
        var s = options.Url!;

        //user:pass@host:port/virtualHost
        s = s[7..];
        var i = s.IndexOf(":", StringComparison.Ordinal);
        options.User = s[..i];

        //pass@host:port/virtualHost
        s = s[++i..];
        i = s.IndexOf("@", StringComparison.Ordinal);
        options.Password = s[..i];

        //host:port/virtualHost
        s = s[++i..];
        i = s.IndexOf(":", StringComparison.Ordinal);
        options.Host = s[..i];

        //port/virtualHost
        s = s[++i..];
        i = s.IndexOf("/", StringComparison.Ordinal);
        options.Port = int.Parse(s[..i]);

        //virtualHost
        s = s[++i..];
        options.VirtualHost = s;
    }

    public static ConnectionFactory CreateMainConnectionFactory(this MQOptions options, int prefetchCount = 1)
    {
        PopulateFromUrl(options);

        return new()
        {
            UserName = options.User,
            Password = options.Password,
            VirtualHost = options.VirtualHost,
            HostName = options.Host,
            Port = options.Port,
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(5),
            DispatchConsumersAsync = true,
            ConsumerDispatchConcurrency = prefetchCount,
            RequestedHeartbeat = TimeSpan.FromSeconds(10)
        };
    }

    public static ConnectionFactory CreateSubConnectionFactory(this MQOptions options)
    {
        PopulateFromUrl(options);

        return new()
        {
            UserName = options.User,
            Password = options.Password,
            VirtualHost = options.VirtualHost,
            HostName = options.Host,
            Port = options.Port,
            AutomaticRecoveryEnabled = true,
            TopologyRecoveryEnabled = false,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(5),
            DispatchConsumersAsync = true,
            RequestedHeartbeat = TimeSpan.FromSeconds(10)
        };
    } 

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

    public static async Task SubBasicPublishAsync(this IModel subModel, string queue, ReadOnlyMemory<byte> buffer, MsgThreshold msg)
    {
        await msg.Wait(() => subModel.MessageCount(queue));
        subModel.BasicPublish("", queue, null!, buffer);
        msg.Add();
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

    public static void TryBasicAck(this IModel model, ulong deliveryTag, ILogger log, bool multiple = false)
    {
        try
        {
            if (model.IsOpen)
                model.BasicAck(deliveryTag, multiple);
        }
        catch (Exception e)
        {
            log.LogWarning(e, null);
        }
    }

    public static void CopyPropertiesFrom(this object toObj, object fromObj)
    {
        var srcPs = fromObj.GetType().GetProperties();
        if (srcPs.Length == 0)
            return;

        var tgtPs = toObj.GetType().GetProperties().ToList();
        tgtPs.RemoveAll(i => !i.CanWrite);
        foreach (var srcP in srcPs)
        {
            var foundTgtP = tgtPs.FirstOrDefault(i => srcP.Name == i.Name);
            if (foundTgtP != null)
                foundTgtP.SetValue(toObj, srcP.GetValue(fromObj, null), null);
        }
    }
}