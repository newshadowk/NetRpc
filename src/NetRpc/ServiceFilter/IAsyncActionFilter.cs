namespace NetRpc;

public interface IAsyncActionFilter
{
    Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next);
}