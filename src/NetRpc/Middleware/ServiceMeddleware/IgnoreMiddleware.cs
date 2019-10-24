using System;
using System.Threading.Tasks;

namespace NetRpc
{
    internal class IgnoreMiddleware
    {
        private readonly RequestDelegate _next;

        public IgnoreMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(ServiceContext context)
        {
            CheckIgnore(context);
            await _next(context);
        }

        private static void CheckIgnore(ServiceContext context)
        {
            switch (context.ChannelType)
            {
                case ChannelType.Undefined:
                    break;
                case ChannelType.Grpc:
                    if (context.ContractMethod.IsGrpcIgnore)
                        throw new NetRpcIgnoreException("Grpc");
                    break;
                case ChannelType.RabbitMQ:
                    if (context.ContractMethod.IsRabbitMQIgnore)
                        throw new NetRpcIgnoreException("RabbitMQ");
                    break;
                case ChannelType.Http:
                    if (context.ContractMethod.IsHttpIgnore)
                        throw new NetRpcIgnoreException("Http");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}