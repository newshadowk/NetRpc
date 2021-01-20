using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NetRpc
{
    public sealed class ProxyStream : CacheStream
    {
        private readonly bool _isManualPosition;
        private readonly Stream _stream;
        private long _manualPosition;
        private bool _readFromCache;

        public ProxyStream(Stream stream)
        {
            try
            {
                Length = stream.Length;
            }
            catch
            {
            }

            _stream = stream;
        }

        public ProxyStream(Stream stream, long length, bool isManualPosition = false)
        {
            _stream = stream;
            Length = length;
            _isManualPosition = isManualPosition;
        }

        public override bool CanRead => _stream.CanRead;

        public override bool CanSeek => _stream.CanSeek;

        public override bool CanWrite => _stream.CanWrite;

        public override long Length { get; }

        public override long Position
        {
            get
            {
                if (_isManualPosition)
                    return _manualPosition;
                return _stream.Position;
            }
            set
            {
                if (_isManualPosition)
                    _manualPosition = value;
                else
                    _stream.Position = value;
            }
        }

        public override void Reset()
        {
            _readFromCache = true;
            _manualPosition = 0;
            base.Reset();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var readCount = 0;
            try
            {
                //read from Cache
                if (_readFromCache)
                {
                    readCount = base.Read(buffer, offset, count);
                    if (readCount == 0)
                        _readFromCache = false;
                }

                //read from _stream
                if (readCount == 0)
                {
                    readCount = _stream.Read(buffer);
                    if (readCount > 0)
                        base.Write(buffer, offset, count);
                }

                if (_isManualPosition)
                    _manualPosition += readCount;
                base.Write(buffer, offset, readCount);
                InvokeStartAsync().AsyncWait();
                OnProgressAsync(new SizeEventArgs(Position)).AsyncWait();
            }
            catch
            {
                InvokeFinishAsync(new SizeEventArgs(Position)).AsyncWait();
                throw;
            }

            if (readCount == 0)
                InvokeFinishAsync(new SizeEventArgs(Position)).AsyncWait();

            return readCount;
        }

        public override int Read(Span<byte> buffer)
        {
            var readCount = 0;
            try
            {
                //read from Cache
                if (_readFromCache)
                {
                    readCount = base.Read(buffer);
                    if (readCount == 0)
                        _readFromCache = false;
                }

                //read from _stream
                if (readCount == 0)
                {
                    readCount = _stream.Read(buffer);
                    if (readCount > 0)
                        base.Write(buffer.Slice(0, readCount));
                }

                if (_isManualPosition)
                    _manualPosition += readCount;
                InvokeStartAsync().AsyncWait();
                OnProgressAsync(new SizeEventArgs(Position)).AsyncWait();
            }
            catch
            {
                InvokeFinishAsync(new SizeEventArgs(Position)).AsyncWait();
                throw;
            }

            if (readCount == 0)
                InvokeFinishAsync(new SizeEventArgs(Position)).AsyncWait();

            return readCount;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var readCount = 0;
            try
            {
                //read from Cache
                if (_readFromCache)
                {
                    readCount = await base.ReadAsync(buffer, offset, count, cancellationToken);
                    if (readCount == 0)
                        _readFromCache = false;
                }

                //read from _stream
                if (readCount == 0)
                {
                    readCount = await _stream.ReadAsync(buffer, offset, count, cancellationToken);
                    if (readCount > 0)
                        await base.WriteAsync(buffer, offset, readCount, cancellationToken);
                }

                if (_isManualPosition)
                    _manualPosition += readCount;
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

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var readCount = 0;
            try
            {
                //read from Cache
                if (_readFromCache)
                {
                    readCount = await base.ReadAsync(buffer, cancellationToken);
                    if (readCount == 0)
                        _readFromCache = false;
                }

                //read from _stream
                if (readCount == 0)
                {
                    readCount = await _stream.ReadAsync(buffer, cancellationToken);
                    if (readCount > 0)
                        await base.WriteAsync(buffer.Slice(0, readCount), cancellationToken);
                }

                if (_isManualPosition)
                    _manualPosition += readCount;
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

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            _stream.Write(buffer);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return _stream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            return _stream.WriteAsync(buffer, cancellationToken);
        }

        public override void Flush()
        {
            _stream.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _stream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _stream.SetLength(value);
        }
    }
}