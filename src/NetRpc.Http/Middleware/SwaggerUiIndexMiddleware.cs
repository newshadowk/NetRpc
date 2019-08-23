using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Writers;

namespace NetRpc.Http
{
    public class SwaggerUiIndexMiddleware
    {
        private readonly Microsoft.AspNetCore.Http.RequestDelegate _next;
        private volatile string _json;
        private volatile string _html;

        public SwaggerUiIndexMiddleware(Microsoft.AspNetCore.Http.RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, INetRpcSwaggerProvider netRpcSwaggerProvider, 
            IOptionsSnapshot<HttpServiceOptions> httpServiceOptions, IOptionsSnapshot<ContractOptions> contractOptions)
        {
            var apiRootApi = httpServiceOptions.Value.ApiRootPath;
            var swaggerRootPath = httpServiceOptions.Value.ApiRootPath + "/swagger";
            var swaggerFilePath = $"{swaggerRootPath}/swagger.json";

            var requestPath = context.Request.Path;

            // api/swagger
            if (IsUrl(requestPath, swaggerRootPath))
            {
                context.Response.Redirect($"{swaggerRootPath}/index.html");
            }
            // api/swagger/index.html
            else if (IsUrl(requestPath, $"{swaggerRootPath}/index.html"))
            {
#if !DEBUG
                if (_html == null)
#endif
                {
                    _html = await ReadStringAsync(".index.html");
                    _html = _html.Replace("{url}", swaggerFilePath);

                    OpenApiDocument doc = netRpcSwaggerProvider.GetSwagger(apiRootApi, contractOptions.Value.InstanceTypes);
                    _json = ToJson(doc);
                }

                context.Response.Headers["Content-Type"] = "text/html; charset=utf-8";
                context.Response.StatusCode = 200;
                await context.Response.WriteAsync(_html);
            }
            // api/swagger/swagger.json
            else if (IsUrl(requestPath, swaggerFilePath))
            {
                await context.Response.WriteAsync(_json);
            }
            //api/swagger/dialog/session.js
            else if (IsUrl(requestPath, $"{swaggerRootPath}/dialog/session.js"))
            {
                var sessionJs = await ReadStringAsync(".dialog.session.js");
                sessionJs = sessionJs.Replace("{hubUrl}", "/callback");
                await context.Response.WriteAsync(sessionJs);
            }
            else
            {
                await _next(context);
            }
        }

        private static bool IsUrl(PathString path, string url)
        {
            return path.HasValue &&
                   string.Equals(path.Value.Trim('/'), url.Trim('/'), StringComparison.OrdinalIgnoreCase);
        }
        
        private static async Task<string> ReadStringAsync(string resourcePath)
        {
            var stream = typeof(SwaggerUiIndexMiddleware).GetTypeInfo().Assembly.GetManifestResourceStream($"{ConstValue.SwaggerUi3Base}{resourcePath}");
            // ReSharper disable once AssignNullToNotNullAttribute
            using (var reader = new StreamReader(stream))
                return await reader.ReadToEndAsync();
        }

        public static string ToJson(OpenApiDocument doc)
        {
            using (var textWriter = new StringWriter())
            {
                var jsonWriter = new OpenApiJsonWriter(textWriter);
                doc.SerializeAsV3(jsonWriter);
                return textWriter.ToString();
            }
        }
    }
}