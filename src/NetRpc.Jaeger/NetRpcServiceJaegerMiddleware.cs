using System;
using System.Reflection;
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
            var info = context.MethodObj.HttpRoutInfo;
            tracer.ActiveSpan.SetTag(new StringTag("Url"),
                $"{options.Value.BasePath.FormatUrl()}/index.html#/{info.ContractPath}/post_{info.ContractPath}_{info.MethodPath}");
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

            var info = context.MethodObj.HttpRoutInfo;
            tracer.ActiveSpan.SetTag(new StringTag("Url"),
                $"{opt.BasePath.FormatUrl()}/index.html#/{info.ContractPath}/post_{info.ContractPath}_{info.MethodPath}");
            await _next(context);
        }
    }
}