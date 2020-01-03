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
            using var ra = new ThrottlingAction(_callbackThrottlingInterval);
            var rawAction = context.Callback;
            context.Callback = o =>
            {
                // ReSharper disable once AccessToDisposedClosure
                ra.Post(() => rawAction(o));
            };
            await _next(context);
        }
    }

    internal sealed class ThrottlingAction : IDisposable
    {
        private readonly BusyTimer _t;
        private Action _action;
        private Action _lastAction;

        private DateTime _lastTime;

        private bool _isEnd;
        private readonly object _lockObj = new object();

        public ThrottlingAction(int intervalMs)
        {
            _t = new BusyTimer(intervalMs);
            _t.Elapsed += TElapsed;
            _t.Start();
        }

        private void TElapsed(object sender, ElapsedEventArgs e)
        {
            Invoke();
        }

        private void Invoke()
        {
            lock (_lockObj)
            {
                if (_isEnd)
                    return;

                if (_action == _lastAction)
                    return;

                _lastAction = _action;
                _lastAction.Invoke();
                _lastTime = DateTime.Now;
            }
        }

        public void Post(Action action)
        {
            lock (_lockObj)
            {
                _action = action;

                if ((DateTime.Now - _lastTime).TotalMilliseconds >= _t.Interval)
                    Invoke();
            }
        }

        public void Dispose()
        {
            lock (_lockObj)
            {
                Invoke();
                _isEnd = true;
                _t?.Dispose();
            }
        }
    }
}