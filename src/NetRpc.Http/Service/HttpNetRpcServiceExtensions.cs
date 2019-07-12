using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;

namespace NetRpc.Http
{
    public static class HttpNetRpcServiceExtensions
    {
        public static IApplicationBuilder UseNetRpcHttp(this IApplicationBuilder app, params object[] instances)
        {
            app.UseNetRpcHttp(null, null, false, instances);
            return app;
        }

        public static IApplicationBuilder UseNetRpcHttp(this IApplicationBuilder app, string rootPath, bool isOpenSwagger, params object[] instances)
        {
            app.UseNetRpcHttp(rootPath, null, isOpenSwagger, instances);
            return app;
        }

        public static IApplicationBuilder UseNetRpcHttp(this IApplicationBuilder app, string rootPath, MiddlewareRegister register, bool isOpenSwagger, params object[] instances)
        {
            if (rootPath == null)
                rootPath = "";

            if (isOpenSwagger)
                app.UseNetRpcSwagger(rootPath, rootPath + "/swagger", instances);

            app.UseMiddleware<HttpNetRpcMiddleware>(rootPath, new RequestHandler(register ?? new MiddlewareRegister(), instances));
            return app;
        }

        private static IApplicationBuilder UseNetRpcSwagger(this IApplicationBuilder app, string apiRootPath, string swaggerRootPath, params object[] instances)
        {
            app.UseMiddleware<SwaggerUiIndexMiddleware>(apiRootPath, swaggerRootPath, instances.ToArray());
            app.UseFileServer(new FileServerOptions
            {
                RequestPath = new PathString(swaggerRootPath),
                FileProvider = new EmbeddedFileProvider(typeof(SwaggerUiIndexMiddleware).GetTypeInfo().Assembly, "NetRpc.Http.SwaggerUi3"),
            });
            return app;
        }
    }
}