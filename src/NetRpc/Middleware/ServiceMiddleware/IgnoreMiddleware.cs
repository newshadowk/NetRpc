using NetRpc.Contract;

namespace NetRpc;

internal class IgnoreMiddleware
{
    private readonly RequestDelegate _next;

    public IgnoreMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(ActionExecutingContext context)
    {
        CheckIgnore(context);
        await _next(context);
    }

    private static void CheckIgnore(ActionExecutingContext context)
    {
        switch (context.ChannelType)
        {
            case ChannelType.Undefined:
                break;
            case ChannelType.Grpc:
                if (context.ContractMethod.IsGrpcIgnore)
                    throw new NIgnoreException(nameof(ChannelType.Grpc));
                break;
            case ChannelType.RabbitMQ:
                if (context.ContractMethod.IsRabbitMQIgnore)
                    throw new NIgnoreException(nameof(ChannelType.RabbitMQ));
                break;
            case ChannelType.Http:
                if (context.ContractMethod.IsHttpIgnore)
                    throw new NIgnoreException(nameof(ChannelType.Http));
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}