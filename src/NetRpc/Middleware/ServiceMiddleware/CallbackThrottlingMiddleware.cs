using System;
using System.Threading.Tasks;
using System.Timers;

namespace NetRpc
{
    public class CallbackThrottlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly int _callbackThrottlingInterval;

        public CallbackThrottlingMiddleware(RequestDelegate next, int callbackThrottlingInterval)
        {
            _next = next;
            _callbackThrottlingInterval = callbackThrottlingInterval;
        }

        public async Task InvokeAsync(ActionExecutingContext context)
        {
            await using ThrottlingFunc ra = new ThrottlingFunc(_callbackThrottlingInterval);
            var rawAction = context.Callback;
            context.Callback = async o =>
            {
                if (rawAction == null)
                    return;

                try
                {
                    // ReSharper disable once AccessToDisposedClosure
                    await ra.PostAsync(() => rawAction(o));
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
                // ReSharper disable once AccessToDisposedClosure
            };
            await _next(context);
        }
    }

    internal sealed class ThrottlingFunc : IAsyncDisposable
    {
        private readonly AsyncLock _lock = new AsyncLock();
        private readonly BusyTimer _t;
        private Func<Task>? _func;
        private Func<Task>? _lastFunc;
        private DateTime _lastTime;
        private bool _isEnd;

        public ThrottlingFunc(int intervalMs)
        {
            _t = new BusyTimer(intervalMs);
            _t.ElapsedAsync += TElapsed;
            _t.Start();
        }

        private async Task TElapsed(object sender, ElapsedEventArgs e)
        {
            await InvokeAsync();
        }

        private async Task InvokeAsync()
        {
            using (await _lock.LockAsync())
            {
                if (_isEnd)
                    return;

                if (_func == _lastFunc)
                    return;

                _lastFunc = _func;
                await _lastFunc!();
                _lastTime = DateTime.Now;
            }
        }

        public async Task PostAsync(Func<Task> func)
        {
            using (await _lock.LockAsync())
            {
                _func = func;
            }

            if ((DateTime.Now - _lastTime).TotalMilliseconds >= _t.Interval)
                await InvokeAsync();
        }

        public async ValueTask DisposeAsync()
        {
            await InvokeAsync();
            _isEnd = true;
            _t?.Dispose();
        }
    }
}