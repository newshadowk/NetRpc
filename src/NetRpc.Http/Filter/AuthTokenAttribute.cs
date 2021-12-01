using System.Security.Authentication;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace NetRpc.Http;

public class AuthTokenAttribute : ActionFilterAttribute
{
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var accessor = (IHttpContextAccessor) context.ServiceProvider.GetService(typeof(IHttpContextAccessor))!;
        if (accessor == null)
            throw new AuthenticationException("IHttpContextAccessor is null.");

        if (accessor.HttpContext == null)
            throw new AuthenticationException("HttpContext is null.");

        if (accessor.HttpContext.User.Identity == null)
            throw new AuthenticationException("HttpContext.User.Identity is null.");

        if (!accessor.HttpContext.User.Identity.IsAuthenticated)
            throw new AuthenticationException();

        await base.OnActionExecutionAsync(context, next);
    }
}