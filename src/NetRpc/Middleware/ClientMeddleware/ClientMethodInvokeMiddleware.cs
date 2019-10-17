using System;
using System.Threading.Tasks;

namespace NetRpc
{
    internal class ClientMethodInvokeMiddleware
    {
        public ClientMethodInvokeMiddleware(ClientRequestDelegate next)
        {
        }

        public async Task InvokeAsync(ClientContext context)
        { 
            context.Result = await context.OnceCall.CallAsync(context.Header, context.MethodInfo, context.Callback, context.Token, context.Stream, context.PureArgs);
        }
    }
}