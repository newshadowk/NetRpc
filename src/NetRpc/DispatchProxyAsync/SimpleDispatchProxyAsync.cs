using System.Threading.Tasks;
using NetRpc;

namespace System.Reflection
{
    public class SimpleDispatchProxyAsync : DispatchProxyAsync
    {
        private IMethodInvoker _invoker;

        public event EventHandler<EventArgsT<Exception>> ExceptionInvoked; 

        private void SetParams(IMethodInvoker invoker)
        {
            _invoker = invoker;
        }

        public static T Create<T>(IMethodInvoker invoker)
        {
            object proxy = Create<T, SimpleDispatchProxyAsync>();
            ((SimpleDispatchProxyAsync)proxy).SetParams(invoker);
            return (T)proxy;
        }

        public override object Invoke(MethodInfo method, object[] args)
        {
            try
            {
                return _invoker.Invoke(method, args);
            }
            catch (Exception e)
            {
                OnExceptionInvoked(new EventArgsT<Exception>(e));
                throw;
            }
        }

        public override async Task InvokeAsync(MethodInfo method, object[] args)
        {
            try
            {
                await _invoker.InvokeAsync(method, args);
            }
            catch (Exception e)
            {
                OnExceptionInvoked(new EventArgsT<Exception>(e));
                throw;
            }
        }

        public override async Task<T> InvokeAsyncT<T>(MethodInfo method, object[] args)
        {
            try
            {
                return await _invoker.InvokeAsyncT<T>(method, args);
            }
            catch (Exception e)
            {
                OnExceptionInvoked(new EventArgsT<Exception>(e));
                throw;
            }
        }

        protected virtual void OnExceptionInvoked(EventArgsT<Exception> e)
        {
            ExceptionInvoked?.Invoke(this, e);
        }
    }
}