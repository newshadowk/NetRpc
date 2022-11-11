using System.Runtime.Serialization;

namespace NetRpc;

[Serializable]
public class BufferException : Exception
{
    public BufferException()
    {
    }

    public BufferException(string message) : base(message)
    {
    }

    protected BufferException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}