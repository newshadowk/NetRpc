using System;
using System.Runtime.Serialization;
using NetRpc.Contract;

namespace NetRpc;

[Serializable]
public class MaxQueueCountException : Exception
{
    public int QueueCount { get; set; }

    public MaxQueueCountException()
    {
    }

    public MaxQueueCountException(int queueCount)
    {
        QueueCount = queueCount;
    }

    protected MaxQueueCountException(SerializationInfo info, StreamingContext context) : base(info, context)
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
        return $"MaxQueueCountException QueueCount:{QueueCount}";
    }
}