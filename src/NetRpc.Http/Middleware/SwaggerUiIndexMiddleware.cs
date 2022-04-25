using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Writers;

namespace NetRpc.Http;

public class SwaggerUiIndexMiddleware
{
    private readonly Microsoft.AspNetCore.Http.RequestDelegate _next;
    private readonly IEnumerable<IInjectSwaggerHtml> _injectSwaggerHtmlList;
    private volatile string _html = null!;
    private readonly Dictionary<string, string> _docJson = new();
    private readonly object _lockDocJson = new();

    public SwaggerUiIndexMiddleware(Microsoft.AspNetCore.Http.RequestDelegate next, IEnumerable<IInjectSwaggerHtml> injectSwaggerHtmlList)
    {
        _next = next;
        _injectSwaggerHtmlList = injectSwaggerHtmlList;
    }

    private string InjectHtml(string html)
    {
        foreach (var injectSwaggerHtml in _injectSwaggerHtmlList)
            html = injectSwaggerHtml.InjectHtml(html);
        return html;
    }

    public async Task Invoke(HttpContext context, INSwaggerProvider nSwaggerProvider, IOptions<HttpServiceOptions> httpServiceOptions,
        IOptions<ContractOptions> contractOptions)
    {
        var apiRootApi = httpServiceOptions.Value.ApiRootPath;
        var swaggerRootPath = httpServiceOptions.Value.ApiRootPath + "/swagger";
        var swaggerFilePath = $"{swaggerRootPath}/swagger.json";
        var requestPath = context.Request.Path;

        string? key = null;
        if (context.Request.Query.TryGetValue("k", out var kValue))
            key = kValue.ToString();

        // api/swagger
        if (IsUrl(requestPath, swaggerRootPath))
        {
            context.Response.Redirect($"{swaggerRootPath}/index.html{context.Request.QueryString}");
        }
        // api/swagger/index.html
        else if (IsUrl(requestPath, $"{swaggerRootPath}/index.html"))
        {
            _html = await ReadStringAsync(".index.html");
            if (key == null)
                _html = _html.Replace("{url}", $"{swaggerFilePath}");
            else
                _html = _html.Replace("{url}", $"{swaggerFilePath}?k={key}");
            _html = InjectHtml(_html);

            lock (_lockDocJson)
            {
                if (!_docJson.ContainsKey(key ?? ""))
                {
                    var doc = nSwaggerProvider.GetSwagger(apiRootApi, contractOptions.Value.Contracts, key);
                    var js = ToJson(doc);
                    _docJson.Add(key ?? "", js);
                }
            }

            context.Response.Headers["Content-Type"] = "text/html; charset=utf-8";
            context.Response.StatusCode = 200;
            await context.Response.WriteAsync(_html);
        }
        // api/swagger/swagger.json
        else if (IsUrl(requestPath, swaggerFilePath))
        {
            string js;
            lock (_lockDocJson)
                js = _docJson[key ?? ""];
            await context.Response.WriteAsync(js);
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
        return path.HasValue && string.Equals(path.Value!.Trim('/'), url.Trim('/'), StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<string> ReadStringAsync(string resourcePath)
    {
        var stream = typeof(SwaggerUiIndexMiddleware).GetTypeInfo().Assembly.GetManifestResourceStream($"{Const.SwaggerUi3Base}{resourcePath}")!;
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    public static string ToJson(OpenApiDocument doc)
    {
        using var textWriter = new StringWriter();
        var jsonWriter = new OpenApiJsonWriter(textWriter);
        doc.SerializeAsV3(jsonWriter);
        return textWriter.ToString();
    }
}