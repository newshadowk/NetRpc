using System;
using System.Threading.Tasks;

namespace NetRpc
{
    public interface IOnceCallFactory : IDisposable
#if NETSTANDARD2_1 || NETCOREAPP3_1
        , IAsyncDisposable
#endif
    {
        Task<IOnceCall> CreateAsync(int timeoutInterval);
    }
}