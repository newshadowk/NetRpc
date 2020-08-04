using System;
using System.Threading.Tasks;

namespace NetRpc
{
    /// <summary>
    /// DI
    /// </summary>
    public interface IOnceCallFactory : IDisposable
    {
        Task<IOnceCall> CreateAsync(int timeoutInterval);
    }
}