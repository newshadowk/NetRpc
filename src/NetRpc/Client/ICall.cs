using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NetRpc
{
    internal interface ICall
    {
        Task<T> CallAsync<T>(ActionInfo action, Action<object> callback, CancellationToken token, System.IO.Stream stream, params object[] args);
    }
}