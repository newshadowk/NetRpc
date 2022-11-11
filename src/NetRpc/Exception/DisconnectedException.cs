using System.Runtime.Serialization;

namespace NetRpc;

[Serializable]
public class DisconnectedException : Exception
{
    public DisconnectedException(string message) : base(message)
    {
    }

    public DisconnectedException()
    {
    }

    protected DisconnectedException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}