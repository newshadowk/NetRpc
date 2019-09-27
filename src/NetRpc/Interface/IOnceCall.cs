using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace NetRpc
{
    public interface IOnceCall
    {
        Task<object> CallAsync(Dictionary<string, object> header, MethodInfo methodInfo, Action<object> callback, CancellationToken token, Stream stream,
            params object[] args);

        Task StartAsync();
    }
}