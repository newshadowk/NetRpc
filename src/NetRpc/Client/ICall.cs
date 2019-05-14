using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NetRpc
{
    internal interface ICall
    {
        Task<T> CallAsync<T>(MethodInfoDto method, Action<object> callback, CancellationToken token, Stream stream, params object[] args);
    }
}