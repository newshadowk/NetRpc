namespace NetRpc;

public static class NullReply
{
    public static ReadOnlyMemory<byte> All { get; } = Reply.FromResult(new CustomResult(null, false, false, 0)).All;
}

internal class Message
{
    private const byte TypeSize = 1;

    public Message(ReadOnlyMemory<byte> bytes)
    {
        if (bytes.Length == TypeSize)
        {
            Body = null;
        }
        else
        {
            Body = bytes.Slice(1);
        }

        Type = bytes.Slice(0, TypeSize).Span[0];
        All = bytes;
    }

    public Message(byte type, ReadOnlyMemory<byte> body)
    {
        Type = type;
        Body = body;

        var allBuffer = new byte[TypeSize + body.Length];
        allBuffer[0] = type;
        if (body.Length > 0)
            Buffer.BlockCopy(body.ToArray(), 0, allBuffer, TypeSize, Body.Length);
        All = allBuffer;
    }

    public byte Type { get; }

    public ReadOnlyMemory<byte> Body { get; }

    public ReadOnlyMemory<byte> All { get; }
}

internal sealed class Request : Message
{
    public new RequestType Type => (RequestType)base.Type;

    public Request(ReadOnlyMemory<byte> bytes) : base(bytes)
    {
    }

    public Request(RequestType type, ReadOnlyMemory<byte> body = default) : base((byte)type, body)
    {
    }
}

internal sealed class Reply : Message
{
    public new ReplyType Type => (ReplyType)base.Type;

    public Reply(ReadOnlyMemory<byte> bytes) : base(bytes)
    {
    }

    private Reply(ReplyType type, ReadOnlyMemory<byte> body = default) : base((byte)type, body)
    {
    }

    public static Reply FromResultStream(long? streamLength)
    {
        return new Reply(ReplyType.ResultStream, streamLength.ToBytes());
    }

    public static Reply FromResult(CustomResult result)
    {
        return new Reply(ReplyType.CustomResult, result.ToBytes());
    }

    public static Reply FromCallback(object? callbackObj)
    {
        return new Reply(ReplyType.Callback, callbackObj.ToBytes());
    }

    public static Reply FromFault(Exception ex)
    {
        return new Reply(ReplyType.Fault, ex.ToBytes());
    }

    public static Reply FromBuffer(ReadOnlyMemory<byte> buffer)
    {
        return new Reply(ReplyType.Buffer, buffer);
    }

    public static Reply FromBufferCancel()
    {
        return new Reply(ReplyType.BufferCancel);
    }

    public static Reply FromBufferFault()
    {
        return new Reply(ReplyType.BufferFault);
    }

    public static Reply FromBufferEnd()
    {
        return new Reply(ReplyType.BufferEnd);
    }
}