using System.Reflection;

namespace NetRpc;

internal interface ICall
{
    Task<object?> CallAsync(MethodInfo methodInfo, bool isRetry, Func<object?, Task>? callback, CancellationToken token, Stream? stream,
        params object?[] pureArgs);
}