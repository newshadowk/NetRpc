using System;
using System.Threading.Tasks;

namespace NetRpc
{
    public interface IOnceCallFactory : IDisposable
    {
        Task<IOnceCall> CreateAsync(int timeoutInterval);
    }
}