using System.Threading.Tasks;

namespace System.Reflection;

public class SimpleDispatchProxyAsync : DispatchProxyAsync
{
    private IMethodInvoker _invoker = null!;

    private void SetParams(IMethodInvoker invoker)
    {
        _invoker = invoker;
    }

    public static T Create<T>(IMethodInvoker invoker) where T : class
    {
        object proxy = Create<T, SimpleDispatchProxyAsync>();
        ((SimpleDispatchProxyAsync) proxy).SetParams(invoker);
        return (T) proxy;
    }

    public override async Task InvokeAsync(MethodInfo method, object?[] args)
    {
        await _invoker.InvokeAsync(method, args);
    }

    public override async Task<T> InvokeAsyncT<T>(MethodInfo method, object?[] args)
    {
        return await _invoker.InvokeAsyncT<T>(method, args);
    }
}