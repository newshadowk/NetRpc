using System;
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
            try
            {
                _length = stream.Length;
            }
            catch
            {
            }

            _stream = stream;
        }

        public ProxyStream(Stream stream, long length)
        {
            _stream = stream;
            _length = length;
        }

#if NETSTANDARD2_1 || NETCOREAPP3_1

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = new CancellationToken())
        {
            int readCount;
            try
            {
                readCount = await _stream.ReadAsync(buffer, cancellationToken);
                await WriteCacheAsync(buffer, cancellationToken);
                await InvokeStartAsync();
                await OnProgressAsync(new SizeEventArgs(Position));
            }
            catch
            {
                await InvokeFinishAsync(new SizeEventArgs(Position));
                throw;
            }

            if (readCount == 0)
                await InvokeFinishAsync(new SizeEventArgs(Position));

            return readCount;
        }

        public override int Read(Span<byte> buffer)
        {
            throw new NotImplementedException();
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = new CancellationToken())
        {
            return _stream.WriteAsync(buffer, cancellationToken);
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            _stream.Write(buffer);
        }

#endif

        public override void Flush()
        {
            _stream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            int readCount;
            try
            {
                readCount = await _stream.ReadAsync(buffer, offset, count, cancellationToken);
                await WriteCacheAsync(buffer, offset, readCount, cancellationToken);
                await InvokeStartAsync();
                await OnProgressAsync(new SizeEventArgs(Position));
            }
            catch
            {
                await InvokeFinishAsync(new SizeEventArgs(Position));
                throw;
            }

            if (readCount == 0)
                await InvokeFinishAsync(new SizeEventArgs(Position));

            return readCount;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _stream.Write(buffer, offset, count);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return _stream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _stream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _stream.SetLength(value);
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