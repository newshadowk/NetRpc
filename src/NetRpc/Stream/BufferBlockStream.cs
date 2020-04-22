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
            int readCount;
            try
            {
                readCount = _reader.Read(buffer, count);
                InvokeStart();
                Position += readCount;
                OnProgress(new SizeEventArgs(Position));
            }
            catch
            {
                InvokeFinish(new SizeEventArgs(Position));
                throw;
            }

            if (readCount == 0)
                InvokeFinish(new SizeEventArgs(Position));

            return readCount;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            int readCount;
            try
            {
                readCount = await _reader.ReadAsync(buffer, count, cancellationToken);
                InvokeStart();
                Position += readCount;
                OnProgress(new SizeEventArgs(Position));
            }
            catch 
            {
                InvokeFinish(new SizeEventArgs(Position));
                throw;
            }

            if (readCount == 0)
                InvokeFinish(new SizeEventArgs(Position));

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
                InvokeFinish(new SizeEventArgs(Position));

            base.Dispose(disposing);
        }
    }
}