using System.Threading.Tasks;

namespace NetRpc;

public interface IAsyncActionFilter
{
    Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next);
}