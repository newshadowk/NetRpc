using System;
using System.Threading;

namespace NetRpc
{
    internal static class ActionHelper
    {
        public static bool Retry(Func<bool> func, int count, int intervalMs)
        {
            for (var i = 0; i < count; i++)
            {
                if (func())
                    return true;
                Thread.Sleep(intervalMs);
            }

            return false;
        }

        public static Action<object> ConvertAction(object action)
        {
            var a = (Delegate) action;
            if (a == null)
                return null;
            return o => a.DynamicInvoke(o);
        }

        public static object ConvertAction(Action<object> action, Type type)
        {
            if (action == null)
                return null;

            var ap = new ActionProxy(action);
            var makeGenericType = typeof(Action<>).MakeGenericType(type);
            // ReSharper disable once PossibleNullReferenceException
            var methodInfo = typeof(ActionProxy).GetMethod("Invoke").MakeGenericMethod(type);
            var @delegate = Delegate.CreateDelegate(makeGenericType, ap, methodInfo);
            return @delegate;
        }

        public class ActionProxy
        {
            private readonly Action<object> _action;

            public ActionProxy(Action<object> action)
            {
                _action = action;
            }

            public void Invoke<T>(T t)
            {
                _action(t);
            }
        }
    }
}