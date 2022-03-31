using System;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;

namespace Proxy.RabbitMQ;

public sealed class BusyTimer : IDisposable
{
    private readonly Timer T = new();
    private volatile bool _isStop;
    public double Interval => T.Interval;

    public BusyTimer(double interval)
    {
        T.Elapsed += T_Elapsed;
        T.Interval = interval;
    }

    public void Dispose()
    {
        _isStop = true;
        T.Dispose();
    }

    public void Start()
    {
        _isStop = false;
        T.Start();
    }

    public void Stop()
    {
        _isStop = true;
        T.Stop();
    }

    public event AsyncEventHandler<ElapsedEventArgs>? ElapsedAsync;

    private Task OnElapsedAsync(ElapsedEventArgs e)
    {
        return ElapsedAsync.InvokeAsync(this, e);
    }

    private async void T_Elapsed(object? sender, ElapsedEventArgs e)
    {
        T.Stop();
 
        try
        {
            await OnElapsedAsync(e);
        }
        catch
        {
        }

        if (!_isStop)
            T.Start();
    }
}