using System.Threading.Tasks;
using static System.Reflection.AsyncDispatchProxyGenerator;

namespace System.Reflection;

public abstract class DispatchProxyAsync
{
    public static T Create<T, TProxy>() where TProxy : DispatchProxyAsync where T : class
    {
        return (T) CreateProxyInstance(typeof(TProxy), typeof(T));
    }

    public abstract Task InvokeAsync(MethodInfo method, object?[] args);

    public abstract Task<T> InvokeAsyncT<T>(MethodInfo method, object?[] args);
}