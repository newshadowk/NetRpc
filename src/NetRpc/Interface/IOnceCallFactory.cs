using System;

namespace NetRpc
{
    public interface IOnceCallFactory : IDisposable
    {
        IOnceCall Create(ContractInfo contract, int timeoutInterval);
    }
}