using System;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;

namespace NetRpc;

public sealed class BusyTimer : IDisposable
{
    private readonly Timer T = new();
    private DateTime _lastTime;
    public double Interval => T.Interval;

    public BusyTimer(double interval)
    {
        T.Elapsed += T_Elapsed;
        T.Interval = interval;
    }

    public void Dispose()
    {
        T.Dispose();
    }

    public void Start()
    {
        T.Start();
    }

    public void Stop()
    {
        T.Stop();
    }

    public event AsyncEventHandler<ElapsedEventArgs>? ElapsedAsync;

    private Task OnElapsedAsync(ElapsedEventArgs e)
    {
        return ElapsedAsync.InvokeAsync(this, e);
    }

    private async void T_Elapsed(object sender, ElapsedEventArgs e)
    {
        if ((DateTime.Now - _lastTime).TotalMilliseconds < Interval)
            return;

        try
        {
            await OnElapsedAsync(e);
        }
        catch
        {
        }
        _lastTime = DateTime.Now;
    }
}
