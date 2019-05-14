using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Threading.Tasks;

namespace NetRpc
{
    public delegate Task RequestDelegate(MiddlewareContext context);

    public class MiddlewareContext : ApiContext
    {
        private object _result;

        public object Result
        {
            get => _result;
            set
            {
                if (value is Task)
                    throw new InvalidCastException("MiddlewareContext Result can not be a Task.");
                _result = value;
            }
        }

        public MiddlewareContext(ApiContext context) : base(context.Header, context.Target, context.Method, context.Args)
        {
        }
    }

    public class MiddlewareRegister
    {
        private MiddlewareBase _current = new MethodInvokeMiddleware(null);

        public void UseMiddleware<TMiddleware>(params object[] args) where TMiddleware : MiddlewareBase
        {
            List<object> newArgs = new List<object>();
            newArgs.Add((RequestDelegate)_current.InvokeAsync);
            newArgs.AddRange(args);
            _current = (TMiddleware) Activator.CreateInstance(typeof(TMiddleware), newArgs.ToArray());
        }
    
        public async Task<object> InvokeAsync(MiddlewareContext context)
        {
            await _current.InvokeAsync(context);
            return context.Result;
        }
    }

    public abstract class MiddlewareBase
    {
        protected readonly RequestDelegate Next;

        public abstract Task InvokeAsync(MiddlewareContext context);

        protected MiddlewareBase(RequestDelegate next)
        {
            Next = next;
        }
    }

    internal class MethodInvokeMiddleware : MiddlewareBase
    {
        public MethodInvokeMiddleware(RequestDelegate next) : base(next) { }

        public override async Task InvokeAsync(MiddlewareContext context)
        {
            var filters = context.Method.GetCustomAttributes(typeof(NetRpcFilterAttribute), true);
            foreach (NetRpcFilterAttribute f in filters)
                await f.InvokeAsync(context);
            NetRpcContext.ThreadHeader.CopyFrom(context.Header);

            dynamic ret;
            try
            {
                // ReSharper disable once PossibleNullReferenceException
                ret = context.Method.Invoke(context.Target, context.Args);
            }
            catch (TargetInvocationException ee)
            {
                if (ee.InnerException != null)
                    throw ee.InnerException;
                throw;
            }

            var isGenericType = context.Method.ReturnType.IsGenericType;
            context.Result = await ApiWrapper.GetTaskResult(ret, isGenericType);
        }
    }
}