using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

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

    public class BufferBlockReader
    {
        private readonly BufferBlock<(byte[], BufferType)> _block;

        private CacheBuffer lastBuffer;

        private bool _isEnd;

        public BufferBlockReader(BufferBlock<(byte[], BufferType)> block)
        {
            _block = block;
        }

        public int Read(byte[] buffer, int count)
        {
            if (_isEnd)
                return 0;

            var readCount = 0;
            while (readCount < count)
            {
                if (lastBuffer == null || lastBuffer.IsEnd)
                {
                    var value = _block.Receive(TimeSpan.FromSeconds(20));
                    if (value.Item2 == BufferType.End ||
                        value.Item2 == BufferType.Fault)
                    {
                        _isEnd = true;
                        return readCount;
                    }

                    if (value.Item2 == BufferType.Cancel)
                    {
                        _isEnd = true;
                        throw new TaskCanceledException();
                    }

                    lastBuffer = new CacheBuffer(value.Item1);
                }

                var readBufferCount = lastBuffer.Read(buffer, readCount, count - readCount);
                readCount += readBufferCount;
            }

            return readCount;
        }
    }

    public sealed class BufferBlockStream : Stream
    {
        private long? _length;
        private readonly BufferBlockReader _reader;
        public event EventHandler End;

        public event EventHandler<long> Progress;

        public BufferBlockStream(BufferBlock<(byte[], BufferType)> block, long? length)
        {
            _reader = new BufferBlockReader(block);
            _length = length;
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var readCount = _reader.Read(buffer, count);
            Position += readCount;
            OnProgress(Position);
            return readCount;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            _length = value;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override bool CanRead { get; } = true;

        public override bool CanSeek { get; } = false;

        public override bool CanWrite { get; } = false;

        public override long Length
        {
            get
            {
                if (_length == null)
                    throw new NotSupportedException();
                return _length.Value;
            }
        }

        public override long Position { get; set; }

        private void OnEnd()
        {
            End?.Invoke(this, EventArgs.Empty);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                OnEnd();
            base.Dispose(disposing);
        }

        private void OnProgress(long e)
        {
            Progress?.Invoke(this, e);
        }
    }
}