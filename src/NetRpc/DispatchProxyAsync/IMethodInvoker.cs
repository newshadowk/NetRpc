using System.Threading.Tasks;

namespace System.Reflection;

public interface IMethodInvoker
{
    Task InvokeAsync(MethodInfo targetMethod, object?[] args);

    Task<T> InvokeAsyncT<T>(MethodInfo targetMethod, object?[] args);
}