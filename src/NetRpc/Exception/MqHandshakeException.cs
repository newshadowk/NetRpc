using System;
using System.Runtime.Serialization;

namespace NetRpc;

[Serializable]
public class MqHandshakeException : Exception
{
    public MqHandshakeException(string? message) : base(message)
    {
    }

    protected MqHandshakeException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}