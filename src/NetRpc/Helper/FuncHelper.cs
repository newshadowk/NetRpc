using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace NetRpc
{
    internal static class FuncHelper
    {
        [return: NotNullIfNotNull("func")]
        public static Func<object?, Task>? ConvertFunc(object? func)
        {
            if (func == null)
                return null;
            var f = (Delegate) func;
            return o => (Task) f.DynamicInvoke(o)!;
        }

        [return: NotNullIfNotNull("func")]
        public static object? ConvertFunc(Func<object?, Task>? func, Type? type)
        {
            if (func == null)
                return null;
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            var fp = new FuncProxy(func);
            var makeGenericType = typeof(Func<,>).MakeGenericType(type, typeof(Task));
            var methodInfo = typeof(FuncProxy).GetMethod("InvokeAsync")!.MakeGenericMethod(type);
            var @delegate = Delegate.CreateDelegate(makeGenericType, fp, methodInfo);
            return @delegate;
        }

        public class FuncProxy
        {
            private readonly Func<object?, Task> _func;

            public FuncProxy(Func<object?, Task> func)
            {
                _func = func;
            }

            public async Task InvokeAsync<T>(T t)
            {
                await _func(t);
            }
        }
    }
}