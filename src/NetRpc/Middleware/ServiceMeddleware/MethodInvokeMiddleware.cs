using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace NetRpc
{
    internal class MethodInvokeMiddleware
    {
        public MethodInvokeMiddleware(RequestDelegate next)
        {
        }

        public async Task InvokeAsync(ServiceContext context)
        {
            var filters = context.InstanceMethodInfo.GetCustomAttributes<NetRpcFilterAttribute>(true);
            foreach (var f in filters)
                await f.InvokeAsync(context);

            dynamic ret;
            try
            {
                // ReSharper disable once PossibleNullReferenceException
                ret = context.InstanceMethodInfo.Invoke(context.Target, context.Args);
            }
            catch (TargetInvocationException e)
            {
                if (e.InnerException != null)
                {
                    var edi = ExceptionDispatchInfo.Capture(e.InnerException);
                    edi.Throw();
                }

                throw;
            }

            var isGenericType = context.InstanceMethodInfo.ReturnType.IsGenericType;
            context.Result = await ApiWrapper.GetTaskResult(ret, isGenericType);
        }
    }
}