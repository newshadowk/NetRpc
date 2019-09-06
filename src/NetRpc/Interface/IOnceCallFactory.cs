using System;

namespace NetRpc
{
    public interface IOnceCallFactory : IDisposable
    {
        IOnceCall<T> Create<T>(Type contractType, int timeoutInterval);
    }
}