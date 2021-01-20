using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NetRpc
{
    public abstract class CacheStream : EventStream
    {
        public Stream? ReadCacheStream { get; private set; }

        public bool TryAttachCache(Stream stream)
        {
            if (ReadCacheStream == null)
            {
                ReadCacheStream = stream;
                return true;
            }
            return false;
        }

        public bool TryAttachCache()
        {
            if (ReadCacheStream == null)
            {
                ReadCacheStream = new MemoryStream();
                return true;
            }
            return false;
        }

        public override void Reset()
        {
            ReadCacheStream?.Seek(0, SeekOrigin.Begin);
            base.Reset();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (ReadCacheStream == null)
                return 0;
            return ReadCacheStream.Read(buffer, offset, count);
        }

        public override int Read(Span<byte> buffer)
        {
            if (ReadCacheStream == null)
                return 0;
            return ReadCacheStream.Read(buffer);
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (ReadCacheStream == null)
                return 0;
            return await ReadCacheStream.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (ReadCacheStream == null)
                return 0;
            return await ReadCacheStream.ReadAsync(buffer, cancellationToken);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            ReadCacheStream?.Write(buffer, offset, count);
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            ReadCacheStream?.Write(buffer);
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (ReadCacheStream == null)
                return;
            await ReadCacheStream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (ReadCacheStream == null)
                return new ValueTask();
            return ReadCacheStream.WriteAsync(buffer, cancellationToken);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                ReadCacheStream?.Dispose();

            base.Dispose(disposing);
        }
    }
}