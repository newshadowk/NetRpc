using System;

namespace NetRpc
{
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
            All[0] = (byte)type;
            if (Body != null)
                Buffer.BlockCopy(body, 0, All, TypeSize, Body.Length);
        }

        public int Type { get; }

        public byte[] Body { get; }

        public byte[] All { get; }
    }


    internal sealed class Request : Message
    {
        public new RequestType Type => (RequestType)Enum.ToObject(typeof(RequestType), base.Type);

        public Request(byte[] bytes) : base(bytes)
        {
        }
   
        public Request(RequestType type, byte[] body = default) : base((int)type, body)
        {
        }
    }

    internal sealed class Reply : Message
    {
        public new ReplyType Type => (ReplyType)Enum.ToObject(typeof(ReplyType), base.Type);

        public Reply(byte[] bytes) : base(bytes)
        {
        }

        public Reply(ReplyType type, byte[] body = default) : base((int)type, body)
        {
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
        ResultStream,
        Result,
        Callback,
        Fault,
        Buffer,
        BufferCancel,
        BufferFault,
        BufferEnd
    }
}