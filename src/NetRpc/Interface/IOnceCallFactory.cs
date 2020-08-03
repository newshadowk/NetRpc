using System;
using System.Threading.Tasks;

namespace NetRpc
{
    public interface IOnceCallFactory : IAsyncDisposable
    {
        Task<IOnceCall> CreateAsync(int timeoutInterval);
    }
}