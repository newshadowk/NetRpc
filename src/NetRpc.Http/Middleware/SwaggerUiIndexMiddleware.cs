using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi.Writers;

namespace NetRpc.Http
{
    public class SwaggerUiIndexMiddleware
    {
        private readonly Microsoft.AspNetCore.Http.RequestDelegate _next;
        private readonly string _apiRootApi;
        private readonly string _swaggerRootPath;
        private readonly string _swaggerFilePath;
        private readonly object[] _instances;
        private readonly string _resourcePath;
        private volatile string _json;
        private volatile string _html;

        public SwaggerUiIndexMiddleware(Microsoft.AspNetCore.Http.RequestDelegate next, string apiRootApi, string swaggerRootPath, object[] instances)
        {
            _next = next;
            _apiRootApi = apiRootApi;
            _swaggerRootPath = swaggerRootPath.Trim('/');
            _resourcePath = "NetRpc.Http.SwaggerUi3.index.html";
            _instances = instances;
            _swaggerFilePath = $"{swaggerRootPath}/swagger.json";
        }

        public async Task Invoke(HttpContext context, INetRpcSwaggerProvider netRpcSwaggerProvider)
        {
            var requestPath = context.Request.Path;

            // api/swagger
            if (requestPath.HasValue &&
                string.Equals(requestPath.Value.Trim('/'), _swaggerRootPath.Trim('/'), StringComparison.OrdinalIgnoreCase))
            {
                context.Response.Redirect($"/{_swaggerRootPath}/index.html");
            }
            // api/swagger/index.html
            else if (requestPath.HasValue &&
                string.Equals(requestPath.Value.Trim('/'), $"{_swaggerRootPath}/index.html", StringComparison.OrdinalIgnoreCase))
            {
                //if (_html == null)
                {
                    var stream = typeof(SwaggerUiIndexMiddleware).GetTypeInfo().Assembly.GetManifestResourceStream(_resourcePath);
                    using (var reader = new StreamReader(stream))
                    {
                        context.Response.Headers["Content-Type"] = "text/html; charset=utf-8";
                        context.Response.StatusCode = 200;
                        _html = await reader.ReadToEndAsync();
                    }
                    var doc = netRpcSwaggerProvider.GetSwagger(_apiRootApi, _instances);
                    using (var textWriter = new StringWriter())
                    {
                        var jsonWriter = new OpenApiJsonWriter(textWriter);
                        doc.SerializeAsV3(jsonWriter);
                        _json = textWriter.ToString();
                        _html = _html.Replace("{url}", _swaggerFilePath);
                    }
                }
                
                await context.Response.WriteAsync(_html);
            }
            // api/swagger/swagger.json
            else if (requestPath.HasValue &&
                     string.Equals(requestPath.Value.Trim('/'), _swaggerFilePath.Trim('/'), StringComparison.OrdinalIgnoreCase))
            {
                await context.Response.WriteAsync(_json);
            }
            else
            {
                await _next(context);
            }
        }
    }
}