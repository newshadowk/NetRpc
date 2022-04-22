using System;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;

namespace Proxy.RabbitMQ;

public sealed class BusyTimer : IDisposable
{
    private readonly Timer T = new();
    private volatile bool _isStop;
    private readonly object _lockStop = new();
    public double Interval => T.Interval;

    public BusyTimer(double interval)
    {
        T.Elapsed += T_Elapsed;
        T.Interval = interval;
    }

    public void Dispose()
    {
        lock (_lockStop)
        {
            T.Dispose();
            _isStop = true;
        }
    }

    public void Start()
    {
        lock (_lockStop)
        {
            T.Start();
            _isStop = false;
        }
    }

    public void Stop()
    {
        lock (_lockStop)
        {
            T.Stop();
            _isStop = true;
        }
    }

    public event AsyncEventHandler<ElapsedEventArgs>? ElapsedAsync;

    public event EventHandler<ElapsedEventArgs>? Elapsed;

    private Task OnElapsedAsync(ElapsedEventArgs e)
    {
        return ElapsedAsync.InvokeAsync(this, e);
    }

    private async void T_Elapsed(object? sender, ElapsedEventArgs e)
    {
        T.Stop();
 
        try
        {
            // ReSharper disable once MethodHasAsyncOverload
            OnElapsed(e);
            await OnElapsedAsync(e);
        }
        catch
        {
        }

        lock (_lockStop)
        {
            if (!_isStop)
                T.Start();
        }
    }

    private void OnElapsed(ElapsedEventArgs e)
    {
        Elapsed?.Invoke(this, e);
    }
}
