namespace NetRpc;

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
            : wait.ContinueWith((_, state) => new Release((AsyncLock)state!),
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

internal class AsyncSemaphore
{
    private static readonly Task SCompleted = Task.FromResult(true);
    private readonly Queue<TaskCompletionSource<bool>> _waiters = new();
    private int _currentCount;

    public AsyncSemaphore(int initialCount)
    {
        if (initialCount < 0)
            throw new ArgumentOutOfRangeException(nameof(initialCount));
        _currentCount = initialCount;
    }

    public Task WaitAsync()
    {
        lock (_waiters)
        {
            if (_currentCount > 0)
            {
                --_currentCount;
                return SCompleted;
            }

            var waiter = new TaskCompletionSource<bool>();
            _waiters.Enqueue(waiter);
            return waiter.Task;
        }
    }

    public void Release()
    {
        TaskCompletionSource<bool>? toRelease = null;
        lock (_waiters)
        {
            if (_waiters.Count > 0)
                toRelease = _waiters.Dequeue();
            else
                ++_currentCount;
        }

        toRelease?.SetResult(true);
    }
}