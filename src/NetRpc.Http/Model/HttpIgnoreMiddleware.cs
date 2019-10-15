using System.Threading.Tasks;

namespace NetRpc.Http
{
    public class HttpIgnoreMiddleware
    {
        private readonly RequestDelegate _next;

        public HttpIgnoreMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(ServiceContext context)
        {
            if (context.MethodObj.HttpIgnore)
                throw new NetRpcIgnoreException();
            await _next(context);
        }
    }
}