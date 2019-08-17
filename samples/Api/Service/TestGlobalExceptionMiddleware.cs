using System;
using System.Threading.Tasks;
using NetRpc;

namespace Service
{
    public class TestGlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public TestGlobalExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
            Console.WriteLine($"[testArg1]");
        }

        public async Task InvokeAsync(RpcContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception e)
            {
                Console.WriteLine($"[log by Middleware] {e.GetType().Name}");
                throw;
            }
        }
    }
}