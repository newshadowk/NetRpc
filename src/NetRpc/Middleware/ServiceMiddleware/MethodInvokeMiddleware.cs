using System.Threading.Tasks;

namespace NetRpc;

internal class MethodInvokeMiddleware
{
    // ReSharper disable once UnusedParameter.Local
    public MethodInvokeMiddleware(RequestDelegate next)
    {
    }

    public async Task InvokeAsync(ActionExecutingContext context)
    {
        var ai = new ActionInvoker(context);
        await ai.InvokeAsync();
    }
}