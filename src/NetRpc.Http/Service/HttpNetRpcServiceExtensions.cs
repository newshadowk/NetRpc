using Microsoft.AspNetCore.Builder;

namespace NetRpc.Http
{
    public static class HttpNetRpcServiceExtensions
    {
        public static IApplicationBuilder UseNetRpcHttp(this IApplicationBuilder app, params object[] instances)
        {
            app.UseMiddleware<HttpNetRpcMiddleware>("", new RequestHandler(new MiddlewareRegister(), instances), true);
            return app;
        }

        public static IApplicationBuilder UseNetRpcHttp(this IApplicationBuilder app, string rootPath, MiddlewareRegister register, params object[] instances)
        {
            if (rootPath == null)
                rootPath = "";
            app.UseMiddleware<HttpNetRpcMiddleware>(rootPath, new RequestHandler(register, instances));
            return app;
        }
    }
}