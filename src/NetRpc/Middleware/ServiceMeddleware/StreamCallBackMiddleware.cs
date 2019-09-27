using System.Threading.Tasks;

namespace NetRpc
{
    public class StreamCallBackMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly int _progressCount;

        public StreamCallBackMiddleware(RequestDelegate next, int progressCount)
        {
            _next = next;
            _progressCount = progressCount;
        }

        public async Task InvokeAsync(ServiceContext context)
        {
            Helper.ConvertStreamProgress(context, _progressCount);
            await _next(context);
        }
    }
}