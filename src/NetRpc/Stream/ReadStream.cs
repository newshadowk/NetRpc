using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NetRpc
{
    public abstract class ReadStream : Stream
    {
        public event AsyncEventHandler<SizeEventArgs>? FinishedAsync;
        public event AsyncEventHandler? StartedAsync;
        public event AsyncEventHandler<SizeEventArgs>? ProgressAsync;

        private volatile bool _isStarted;
        private volatile bool _isFinished;

        public Stream? CacheStream { get; set; }

        protected void WriteCache(byte[] buffer, int offset, int count)
        {
            CacheStream?.Write(buffer, offset, count);
        }

        protected async Task WriteCacheAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (CacheStream == null)
                return;
            await CacheStream.WriteAsync(buffer, offset, count, cancellationToken);
        }

#if NETSTANDARD2_1 || NETCOREAPP3_1
        protected ValueTask WriteCacheAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
        {
            if (CacheStream == null)
                return new ValueTask();
            return CacheStream.WriteAsync(buffer, cancellationToken);
        }

        protected void WriteCache(ReadOnlySpan<byte> buffer)
        {
            CacheStream?.Write(buffer);
        }
#endif

        protected async Task InvokeFinishAsync(SizeEventArgs e)
        {
            if (!_isStarted)
                return;

            if (!_isFinished)
            {
                _isFinished = true;
                await OnFinishedAsync(e);
            }
        }

        protected async Task InvokeStartAsync()
        {
            if (!_isStarted)
            {
                _isStarted = true;
                await OnStartedAsync();
            }
        }

        protected virtual Task OnStartedAsync()
        {
            return StartedAsync.InvokeAsync(this, EventArgs.Empty);
        }

        protected virtual Task OnFinishedAsync(SizeEventArgs e)
        {
            return FinishedAsync.InvokeAsync(this, e);
        }

        protected virtual Task OnProgressAsync(SizeEventArgs e)
        {
            return ProgressAsync.InvokeAsync(this, e);
        }
    }
}