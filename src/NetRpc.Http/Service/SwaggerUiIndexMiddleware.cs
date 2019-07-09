using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace NetRpc.Http
{
    public class SwaggerUiIndexMiddleware
    {
        private readonly Microsoft.AspNetCore.Http.RequestDelegate _next;
        private readonly string _swaggerRootPath;
        private readonly object[] _instances;
        private readonly string _resourcePath;

        public SwaggerUiIndexMiddleware(Microsoft.AspNetCore.Http.RequestDelegate next, string swaggerRootPath, string resourcePath)
        {
            _next = next;
            _swaggerRootPath = swaggerRootPath;
            //_instances = instances;
            _resourcePath = resourcePath;
        }

        public async Task Invoke(HttpContext context)
        {
            if (context.Request.Path.HasValue && string.Equals(context.Request.Path.Value.Trim('/'), _swaggerRootPath.Trim('/'), StringComparison.OrdinalIgnoreCase))
            {
                var stream = typeof(SwaggerUiIndexMiddleware).GetTypeInfo().Assembly.GetManifestResourceStream(_resourcePath);
                using (var reader = new StreamReader(stream))
                {
                    context.Response.Headers["Content-Type"] = "text/html; charset=utf-8";
                    context.Response.StatusCode = 200;
                    await context.Response.WriteAsync(await reader.ReadToEndAsync());
                }
            }
            else
            {
                await _next(context);
            }
        }
    }
}