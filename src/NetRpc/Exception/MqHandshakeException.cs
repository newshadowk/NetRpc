using System.Runtime.Serialization;
using NetRpc.Contract;

namespace NetRpc;

[Serializable]
[NotRetry]
public class MqHandshakeException : Exception
{
    public int QueueCount { get; set; }

    public MqHandshakeException()
    {
    }

    public MqHandshakeException(int queueCount)
    {
        QueueCount = queueCount;
    }

    protected MqHandshakeException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
        this.SetObjectData(info);
    }

    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        base.GetObjectData(info, context);
        this.GetObjectData(info);
    }

    public override string ToString()
    {
        return $"MqHandshakeException QueueCount:{QueueCount}";
    }
}