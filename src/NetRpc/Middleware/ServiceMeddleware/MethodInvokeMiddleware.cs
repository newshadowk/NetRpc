using System;
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
            ActionInvoker ai = new ActionInvoker(context);
            await ai.InvokeAsync();
        }
    }
}