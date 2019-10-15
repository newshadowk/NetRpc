using System.Threading.Tasks;

namespace NetRpc.Grpc
{
    public class GrpcIgnoreMiddleware
    {
        private readonly RequestDelegate _next;

        public GrpcIgnoreMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(ServiceContext context)
        {
            if (context.MethodObj.GrpcIgnore)
                throw new NetRpcIgnoreException();
            await _next(context);
        }
    }
}