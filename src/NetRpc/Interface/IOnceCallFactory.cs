using System;
using System.Threading.Tasks;

namespace NetRpc
{
    /// <summary>
    /// DI
    /// </summary>
    public interface IOnceCallFactory : IDisposable
    {
        /// <param name="timeoutInterval"></param>
        /// <param name="isRetry">if true, recreate connection that can redirect to other node, e.g retry the grpc http2 connection.</param>
        Task<IOnceCall> CreateAsync(int timeoutInterval, bool isRetry);
    }
}