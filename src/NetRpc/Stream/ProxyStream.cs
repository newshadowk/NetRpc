using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NetRpc
{
    public sealed class ProxyStream : ReadStream
    {
        private readonly Stream _stream;
        private readonly long _length;

        public ProxyStream(Stream stream)
        {
            _stream = stream;
        }

        public ProxyStream(Stream stream, long length)
        {
            _stream = stream;
            _length = length;
        }

        public override void Flush()
        {
            _stream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            InvokeStart();

            var readCount = _stream.Read(buffer, offset, count);

            OnProgress(Position);

            if (readCount < count)
                InvokeFinish();

            return readCount;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            InvokeStart();

            var readCount = await _stream.ReadAsync(buffer, offset, count, cancellationToken);

            OnProgress(Position);

            if (readCount < count)
                InvokeFinish();

            return readCount;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _stream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _stream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _stream.Write(buffer, offset, count);
        }

        public override bool CanRead => _stream.CanRead;
        public override bool CanSeek => _stream.CanSeek;
        public override bool CanWrite => _stream.CanWrite;

        // ReSharper disable once ConvertToAutoProperty
        public override long Length => _length;

        public override long Position
        {
            get => _stream.Position;
            set => _stream.Position = value;
        }
    }
}