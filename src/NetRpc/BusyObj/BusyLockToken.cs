using System;

namespace NetRpc;

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