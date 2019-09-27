using System.Threading.Tasks;
using OpenTracing;
using OpenTracing.Propagation;

namespace NetRpc.OpenTracing
{
    public class NetRpcOpenTracingMiddleware
    {
        private readonly RequestDelegate _next;

        public NetRpcOpenTracingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(ServiceContext context, ITracer tracer)
        {
            var spanContext = tracer.Extract(BuiltinFormats.HttpHeaders, new RequestHeadersExtractAdapter(context.Header));
            using (var scope = tracer.BuildSpan(context.ContractMethodInfo.Name)
                .AsChildOf(spanContext)
                .StartActive(true))
            {
                await _next(context);
            }
        }
    }
}