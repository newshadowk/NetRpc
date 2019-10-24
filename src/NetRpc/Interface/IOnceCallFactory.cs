using System;

namespace NetRpc
{
    public interface IOnceCallFactory : IDisposable
    {
        IOnceCall Create(int timeoutInterval);
    }
}