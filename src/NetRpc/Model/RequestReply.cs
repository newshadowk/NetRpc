using System;

namespace NetRpc
{
    public static class NullReply
    {
        public static byte[] All { get; } = Reply.FromResult(new CustomResult(null, false, 0)).All;
    }

    internal class Message
    {
        private const int TypeSize = 1;

        public Message(byte[] bytes)
        {
            var typeBytes = new byte[TypeSize];

            if (bytes.Length == TypeSize)
            {
                Body = null;
            }
            else
            {
                Body = new byte[bytes.Length - TypeSize];
                Buffer.BlockCopy(bytes, TypeSize, Body, 0, Body.Length);
            }

            Buffer.BlockCopy(bytes, 0, typeBytes, 0, TypeSize);
            Type = typeBytes[0];

            All = bytes;
        }

        public Message(int type, byte[] body)
        {
            Type = type;
            Body = body;
            All = new byte[TypeSize + (Body?.Length ?? 0)];
            All[0] = (byte) type;
            if (Body != null)
                Buffer.BlockCopy(body, 0, All, TypeSize, Body.Length);
        }

        public int Type { get; }

        public byte[] Body { get; }

        public byte[] All { get; }
    }

    internal sealed class Request : Message
    {
        public new RequestType Type => (RequestType) Enum.ToObject(typeof(RequestType), base.Type);

        public Request(byte[] bytes) : base(bytes)
        {
        }

        public Request(RequestType type, byte[] body = default) : base((int) type, body)
        {
        }
    }

    internal sealed class Reply : Message
    {
        public new ReplyType Type => (ReplyType) Enum.ToObject(typeof(ReplyType), base.Type);

        public Reply(byte[] bytes) : base(bytes)
        {
        }

        private Reply(ReplyType type, byte[] body = default) : base((int) type, body)
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

        public static Reply FromCallback(object callbackObj)
        {
            return new Reply(ReplyType.Callback, callbackObj.ToBytes());
        }

        public static Reply FromFault(Exception ex)
        {
            return new Reply(ReplyType.Fault, ex.ToBytes());
        }

        public static Reply FromBuffer(byte[] buffer)
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

    internal enum RequestType
    {
        Cmd,
        Buffer,
        BufferEnd,
        Cancel
    }

    internal enum ReplyType
    {
        /// <summary>
        /// body:long?, stream length.
        /// </summary>
        ResultStream,

        /// <summary>
        /// body:CustomResult.
        /// </summary>
        CustomResult,

        /// <summary>
        /// body:Callback object.
        /// </summary>
        Callback,

        /// <summary>
        /// body:Exception.
        /// </summary>
        Fault,

        /// <summary>
        /// body:byte[].
        /// </summary>
        Buffer,

        /// <summary>
        /// body:null.
        /// </summary>
        BufferCancel,

        /// <summary>
        /// body:null.
        /// </summary>
        BufferFault,

        /// <summary>
        /// body:null.
        /// </summary>
        BufferEnd
    }
}