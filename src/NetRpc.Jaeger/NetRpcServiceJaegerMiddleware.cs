using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using OpenTracing;
using OpenTracing.Tag;

namespace NetRpc.Jaeger
{
    public class NetRpcServiceJaegerMiddleware
    {
        private readonly RequestDelegate _next;

        public NetRpcServiceJaegerMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(ServiceContext context, ITracer tracer, IOptions<ServiceSwaggerOptions> options)
        {
            if (options.Value.IsPropertiesDefault())
                return;

            //http://localhost:5001/swagger/index.html#/IService/post_IService_Call
            tracer.ActiveSpan.SetTag(new StringTag("Url"),
                $"{options.Value.BasePath}/index.html#/{context.Contract.ContractType.Name}/post_{context.Contract.ContractType.Name}_{context.ContractMethodInfo.Name}");
            await _next(context);
        }
    }

    public class NetRpcClientJaegerMiddleware
    {
        private readonly ClientRequestDelegate _next;

        public NetRpcClientJaegerMiddleware(ClientRequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(ClientContext context, ITracer tracer, IOptionsMonitor<ClientSwaggerOptions> options)
        {
            var opt = options.Get(context.OptionsName);

            if (opt.IsPropertiesDefault())
                return;

            tracer.ActiveSpan.SetTag(new StringTag("Url"),
                $"{opt.BasePath}/index.html#/{context.ContractInfo.Type.Name}/post_{context.ContractInfo.Type.Name}_{context.MethodInfo.Name}");
            await _next(context);
        }
    }
}