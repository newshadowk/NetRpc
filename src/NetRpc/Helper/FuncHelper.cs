using System;
using System.Threading.Tasks;

namespace NetRpc
{
    internal static class FuncHelper
    {
        public static Func<object, Task> ConvertFunc(object func)
        {
            var f = (Delegate)func;
            if (f == null)
                return null;
            return o => (Task) f.DynamicInvoke(o);
        }

        public static object ConvertFunc(Func<object, Task> func, Type type)
        {
            if (func == null)
                return null;

            var fp = new FuncProxy(func);
            var makeGenericType = typeof(Func<,>).MakeGenericType(type, typeof(Task));
            // ReSharper disable once PossibleNullReferenceException
            var methodInfo = typeof(FuncProxy).GetMethod("InvokeAsync").MakeGenericMethod(type);
            var @delegate = Delegate.CreateDelegate(makeGenericType, fp, methodInfo);
            return @delegate;
        }

        public class FuncProxy
        {
            private readonly Func<object, Task> _func;

            public FuncProxy(Func<object, Task> func)
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