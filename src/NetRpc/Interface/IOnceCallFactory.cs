using System;

namespace NetRpc
{
    public interface IOnceCallFactory : IDisposable
    {
        IOnceCall<T> Create<T>(ContractInfo contract, int timeoutInterval);
    }
}