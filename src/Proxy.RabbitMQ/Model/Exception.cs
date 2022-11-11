namespace Proxy.RabbitMQ;

public class MaxQueueCountInnerException : Exception
{
    public int QueueCount { get; set; }

    public MaxQueueCountInnerException(int queueCount)
    {
        QueueCount = queueCount;
    }
}

public class MqHandshakeInnerException : Exception
{
    public int QueueCount { get; set; }

    public MqHandshakeInnerException(int queueCount)
    {
        QueueCount = queueCount;
    }
}