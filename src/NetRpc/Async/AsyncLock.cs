using System;
using System.Threading;
using System.Threading.Tasks;

namespace NetRpc
{
    public class AsyncLock
    {
        private readonly AsyncSemaphore _semaphore;
        private readonly Task<Release> _release;

        public AsyncLock()
        {
            _semaphore = new AsyncSemaphore(1);
            _release = Task.FromResult(new Release(this));
        }

        public Task<Release> LockAsync()
        {
            var wait = _semaphore.WaitAsync();
            return wait.IsCompleted
                ? _release
                : wait.ContinueWith((_, state) => new Release((AsyncLock) state!),
                    this, CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }

        public readonly struct Release : IDisposable
        {
            private readonly AsyncLock _toRelease;

            internal Release(AsyncLock toRelease)
            {
                _toRelease = toRelease;
            }

            public void Dispose()
            {
                _toRelease._semaphore.Release();
            }
        }
    }
}