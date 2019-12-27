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

    internal sealed class BusyLockToken : IDisposable
    {
        private readonly BusyLock _busyLock;

        public BusyLockToken(bool lockGot, BusyLock busyLock)
        {
            LockGot = lockGot;
            _busyLock = busyLock;
        }

        public bool LockGot { get; }

        public void Dispose()
        {
            if (LockGot)
                _busyLock.Release();
        }
    }

    internal sealed class BusyLock
    {
        private readonly object _lockObj = new object();

        private volatile bool _isLocked;

        public BusyLockToken Lock()
        {
            lock (_lockObj)
            {
                if (_isLocked)
                    return new BusyLockToken(false, this);
                _isLocked = true;
                return new BusyLockToken(true, this);
            }
        }

        public void Release()
        {
            lock (_lockObj)
            {
                _isLocked = false;
            }
        }
    }

    internal class BusyTimer : IDisposable
    {
        private readonly Timer T = new Timer();
        private readonly BusyLock TLock = new BusyLock();

        public double Interval => T.Interval;

        public BusyTimer(double interval)
        {
            T.Elapsed += T_Elapsed;
            T.Interval = interval;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            T.Dispose();
        }

        public void Start(bool isImmediately = false)
        {
            TryStart();
            if (isImmediately)
                DoElapsed(null);
        }

        public void Stop()
        {
            TryStop();
        }

        public event ElapsedEventHandler Elapsed;

        private void TryStart()
        {
            //Possible T is already Disposed.
            try
            {
                T.Start();
            }
            catch
            {
            }
        }

        private void TryStop()
        {
            //Possible T is already Disposed.
            try
            {
                T.Stop();
            }
            catch
            {
            }
        }

        private void OnElapsed(ElapsedEventArgs e)
        {
            Elapsed?.Invoke(this, e);
        }

        private void DoElapsed(ElapsedEventArgs e)
        {
            using (var token = TLock.Lock())
            {
                if (!token.LockGot)
                    return;
                TryStop();
                OnElapsed(e);
                TryStart();
            }
        }

        private void T_Elapsed(object sender, ElapsedEventArgs e)
        {
            DoElapsed(e);
        }

        ~BusyTimer()
        {
            Dispose(false);
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