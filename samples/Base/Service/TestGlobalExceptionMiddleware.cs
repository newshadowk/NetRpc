using System;
using System.Threading.Tasks;
using Nrpc;

namespace Service
{
    public class TestGlobalExceptionMiddleware : MiddlewareBase
    {
        public TestGlobalExceptionMiddleware(RequestDelegate next, string arg1) : base(next)
        {
            Console.WriteLine($"[testArg1] {arg1}");
        }

        public override async Task InvokeAsync(MiddlewareContext context)
        {
            try
            {
                await Next(context);
            }
            catch (Exception e)
            {
                Console.WriteLine($"[log by Middleware] {e.GetType().Name}");
                throw;
            }
        }
    }
}