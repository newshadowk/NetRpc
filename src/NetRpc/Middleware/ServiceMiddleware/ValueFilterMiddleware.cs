namespace NetRpc;

public class ValueFilterMiddleware
{
    private readonly RequestDelegate _next;

    public ValueFilterMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(ActionExecutingContext context)
    {
        await context.ValueItemWrapper.ValueFilterInvokeAsync();
        await _next(context);
    }
}