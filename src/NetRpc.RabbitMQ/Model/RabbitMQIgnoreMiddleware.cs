using System.Threading.Tasks;

namespace NetRpc.RabbitMQ
{
    public class RabbitMQIgnoreMiddleware
    {
        private readonly RequestDelegate _next;

        public RabbitMQIgnoreMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(ServiceContext context)
        {
            if (context.MethodObj.RabbitMQIgnore)
                throw new NetRpcIgnoreException();
            await _next(context);
        }
    }
}