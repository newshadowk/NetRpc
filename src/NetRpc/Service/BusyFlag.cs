namespace NetRpc;

public sealed class BusyFlag
{
    private int _handlingCount;

    public bool IsHandling => _handlingCount > 0;

    public int GetCount()
    {
        return _handlingCount;
    }

    public int Increment()
    {
        return Interlocked.Increment(ref _handlingCount);
    }

    public int Decrement()
    {
        return Interlocked.Decrement(ref _handlingCount);
    }
}