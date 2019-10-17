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
            {
                await _next(context);
                return;
            }

            //http://localhost:5001/swagger/index.html#/IService/post_IService_Call
            var info = context.MethodObj.HttpRoutInfo;
            var requestUrl = Helper.GetRequestUrl(options.Value.HostPath.FormatUrl(), options.Value.ApiPath, info.ContractPath, info.MethodPath);
            tracer.ActiveSpan.SetTag(new StringTag("Url"), requestUrl);

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
            {
                await _next(context);
                return;
            }

            var info = context.MethodObj.HttpRoutInfo;
            var requestUrl = Helper.GetRequestUrl(opt.HostPath.FormatUrl(), opt.ApiPath, info.ContractPath, info.MethodPath);
            tracer.ActiveSpan.SetTag(new StringTag("Url"), requestUrl);
            await _next(context);
        }
    }
}