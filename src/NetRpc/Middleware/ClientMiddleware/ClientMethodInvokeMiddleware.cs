using System.Threading.Tasks;

namespace NetRpc
{
    internal class ClientMethodInvokeMiddleware
    {
        // ReSharper disable once UnusedParameter.Local
        public ClientMethodInvokeMiddleware(ClientRequestDelegate next)
        {
        }

        public async Task InvokeAsync(ClientActionExecutingContext context)
        {
            context.Result = await context.OnceCall.CallAsync(context.Header, new MethodContext(context.ContractMethod, context.InstanceMethod), 
                context.Callback, context.CancellationToken, context.Stream, context.PureArgs);
        }
    }
}