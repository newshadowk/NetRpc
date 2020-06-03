using System.Security.Authentication;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace NetRpc.Http
{
    internal class AuthMiddleware
    {
        private readonly RequestDelegate _next;

        public AuthMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(ActionExecutingContext context)
        {
            if (context.Properties.TryGetValue("HttpContext", out var v))
            {
                var hc = (HttpContext) v;
                if (!hc.User.Identity.IsAuthenticated)
                    throw new AuthenticationException();
            }
            else
                throw new AuthenticationException();

            await _next(context);
        }
    }
}