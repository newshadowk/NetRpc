using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace NetRpc
{
    public sealed class BufferBlockStream : ReadStream
    {
        private long _length;
        private readonly BufferBlockReader _reader;

        public BufferBlockStream(BufferBlock<(byte[], BufferType)> block, long length)
        {
            _reader = new BufferBlockReader(block);
            _length = length;
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            InvokeStart();

            var readCount = _reader.Read(buffer, count);
            Position += readCount;
            OnProgress(Position);

            if (readCount < count)
                InvokeFinish();

            return readCount;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            InvokeStart();

            var readCount =  await _reader.ReadAsync(buffer, count, cancellationToken);
            Position += readCount;
            OnProgress(Position);

            if (readCount < count)
                InvokeFinish();

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

        public override long Length => _length;

        public override long Position { get; set; }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                InvokeFinish();

            base.Dispose(disposing);
        }
    }
}