using System.Runtime.Serialization;

namespace NetRpc;

[Serializable]
public class ReplyRepeatException : Exception
{
    public ReplyRepeatException()
    {
    }

    protected ReplyRepeatException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}