using System;
using System.Threading.Tasks;

namespace NetRpc
{
#if NETSTANDARD2_1 || NETCOREAPP3_1
    public interface IOnceCallFactory : IDisposable, IAsyncDisposable
#else
    public interface IOnceCallFactory : IDisposable
#endif
    {
        Task<IOnceCall> CreateAsync(int timeoutInterval);
    }
}