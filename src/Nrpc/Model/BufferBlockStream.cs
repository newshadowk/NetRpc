using System;
using System.IO;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Nrpc
{
    public sealed class FastBufferBlockStream : Stream
    {
        private readonly BufferBlock<(byte[], BufferType)> _block;

        private bool _isEnd;

        public FastBufferBlockStream(BufferBlock<(byte[], BufferType)> block)
        {
            _block = block;
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_isEnd)
                return 0;

            (byte[] data, BufferType type) = _block.Receive(TimeSpan.FromSeconds(1000));
            
            if (type == BufferType.End ||
                type == BufferType.Fault)
            {
                _isEnd = true;
            }
            else if (type == BufferType.Cancel)
            {
                _isEnd = true;
                throw new TaskCanceledException();
            }

            data.CopyTo(buffer, 0);
            Position += data.Length;
            return data.Length;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override bool CanRead { get; } = true;

        public override bool CanSeek { get; } = false;

        public override bool CanWrite { get; } = false;

        public override long Length => throw new NotImplementedException();

        public override long Position { get; set; }
    }

    public sealed class BufferBlockStream : Stream
    {
        private readonly TransformManyBlock<(byte[], BufferType), (byte, BufferType)> _byteBlock =
            new TransformManyBlock<(byte[], BufferType), (byte, BufferType)>(data =>
            {
                if (data.Item1 == null)
                    return new (byte, BufferType)[] { (default, data.Item2) };

                var ret = new (byte, BufferType)[data.Item1.Length];
                for (int i = 0; i < data.Item1.Length; i++)
                {
                    ret[i].Item1 = data.Item1[i];
                    ret[i].Item2 = data.Item2;
                }

                return ret;
            });

        private bool _isEnd;

        public BufferBlockStream(BufferBlock<(byte[], BufferType)> block)
        {
            block.LinkTo(_byteBlock);
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_isEnd)
                return 0;

            int currLength = 0;
            for (int i = 0; i < count; i++)
            {
                (byte, BufferType) value = _byteBlock.Receive();
                if (value.Item2 == BufferType.End ||
                    value.Item2 == BufferType.Fault)
                {
                    _isEnd = true;
                    Position += currLength;
                    return currLength;
                }

                if (value.Item2 == BufferType.Cancel)
                {
                    _isEnd = true;
                    throw new TaskCanceledException();
                }

                buffer[i] = value.Item1;
                currLength = i + 1;
            }

            Position += currLength;
            return currLength;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override bool CanRead { get; } = true;

        public override bool CanSeek { get; } = false;

        public override bool CanWrite { get; } = false;

        public override long Length => throw new NotImplementedException();

        public override long Position { get; set; }
    }
}