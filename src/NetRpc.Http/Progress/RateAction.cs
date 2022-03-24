using System;
using System.Timers;

namespace NetRpc.Http;

internal sealed class RateAction : IDisposable
{
    private readonly Timer _t;
    private Action? _action;
    private Action? _lastAction;

    private DateTime _lastTime;

    private bool _isEnd;
    private readonly object _lockObj = new();

    public RateAction(int intervalMs)
    {
        _t = new Timer(intervalMs);
        _t.Elapsed += TElapsed;
        _t.Start();
    }

    private void TElapsed(object? sender, ElapsedEventArgs e)
    {
        Invoke();
    }

    private void Invoke()
    {
        lock (_lockObj)
        {
            if (_isEnd)
                return;

            if (_action == null)
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
            _t.Dispose();
        }
    }
}