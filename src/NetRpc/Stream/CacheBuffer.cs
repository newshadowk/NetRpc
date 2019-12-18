using System;

namespace NetRpc
{
    public class CacheBuffer
    {
        public byte[] Body { get; }

        public int Position { get; private set; }

        public bool IsEnd => Position >= Body.Length;

        public int LeftCount => Body.Length - Position;

        public CacheBuffer(byte[] body)
        {
            Body = body;
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            var readCount = Math.Min(count, LeftCount);
            Buffer.BlockCopy(Body, Position, buffer, offset, readCount);
            Position += readCount;
            return readCount;
        }
    }
}