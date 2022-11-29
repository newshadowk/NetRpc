using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace NetRpc.Http;

internal interface IHttpObjProcessor
{
    bool MatchContentType(string? contentType);

    Task<HttpObj> ProcessAsync(ProcessItem item);
}

/// <summary>
/// multipart/form-data
/// </summary>
internal sealed class FormDataHttpObjProcessor : IHttpObjProcessor
{
    private readonly FormOptions _defaultFormOptions = new();

    public bool MatchContentType(string? contentType)
    {
        if (contentType == null)
            return false;
        return contentType.StartsWith("multipart/form-data");
    }

    public async Task<HttpObj> ProcessAsync(ProcessItem item)
    {
        var boundary = MultipartRequestHelper.GetBoundary(
            MediaTypeHeaderValue.Parse(item.HttpRequest.ContentType),
            _defaultFormOptions.MultipartBoundaryLengthLimit);
        var reader = new MultipartReader(boundary, item.HttpRequest.Body);

        //body
        HttpDataObj dataObj;
        if (item.DataObjTypeWithoutPathQueryStream != null)
        {
            var bodySec = await reader.ReadNextSectionAsync();
            ValidateSection(bodySec);
            var ms = new MemoryStream();
            await bodySec!.Body.CopyToAsync(ms);
            var body = Encoding.UTF8.GetString(ms.ToArray());
            dataObj = Helper.ToHttpDataObj(body, item.DataObjTypeWithoutPathQueryStream!);
        }
        else if (item.DataObjType != null)
            dataObj = new HttpDataObj { Type = item.DataObjType };
        else
            dataObj = new HttpDataObj();

        //stream
        var streamSec = await reader.ReadNextSectionAsync();
        ValidateSection(streamSec);
        var fileName = GetFileName(streamSec!.ContentDisposition);
        if (fileName == null)
            throw new ArgumentNullException("", "File name is null.");
        dataObj.TrySetStreamName(fileName);
        var proxyStream = new ProxyStream(streamSec.Body, dataObj.StreamLength);
        return new HttpObj { HttpDataObj = dataObj, ProxyStream = proxyStream };
    }

    private static string? Match(string src, string left, string right)
    {
        var r = Regex.Match(src, $"(?<={left}).+(?={right})");
        if (r.Captures.Count > 0)
            return r.Captures[0].Value;
        return null;
    }

    private static string? GetFileName(string? contentDisposition)
    {
        if (contentDisposition == null)
            return null;

        //Content-Disposition: form-data; name="stream"; filename="t1.docx"
        //Content-Disposition: form-data; name="stream"; filename*=utf-8''t1.docx

        var i = contentDisposition.IndexOf("*=utf-8''", StringComparison.Ordinal);
        if (i != -1)
            return contentDisposition.Substring(contentDisposition.IndexOf("*=utf-8''", StringComparison.Ordinal) + 9);
        return Match(contentDisposition, "filename=\"", "\"");
    }

    private static void ValidateSection(MultipartSection? section)
    {
        if (section == null)
            throw new HttpFailedException("ValidateSection, section is null.");

        var hasContentDispositionHeader =
            ContentDispositionHeaderValue.TryParse(
                section.ContentDisposition, out _);

        if (!hasContentDispositionHeader)
            throw new HttpFailedException("ValidateSection, Has not ContentDispositionHeader.");
    }
}

/// <summary>
/// application/json
/// </summary>
internal sealed class JsonHttpObjProcessor : IHttpObjProcessor
{
    public bool MatchContentType(string? contentType)
    {
        if (contentType == null)
            return false;
        return contentType.StartsWith("application/json");
    }

    public async Task<HttpObj> ProcessAsync(ProcessItem item)
    {
        if (item.DataObjType == null)
            return new HttpObj();

        string body;
        using (var sr = new StreamReader(item.HttpRequest.Body, Encoding.UTF8))
            body = await sr.ReadToEndAsync();

        var dataObj = Helper.ToHttpDataObj(body, item.DataObjType!);
        return new HttpObj { HttpDataObj = dataObj };
    }
}

/// <summary>
/// application/x-www-form-urlencoded
/// </summary>
internal sealed class FormUrlEncodedObjProcessor : IHttpObjProcessor
{
    public bool MatchContentType(string? contentType)
    {
        return contentType == "application/x-www-form-urlencoded" ||
               string.IsNullOrWhiteSpace(contentType);
    }

    public Task<HttpObj> ProcessAsync(ProcessItem item)
    {
        if (item.DataObjType == null)
            return Task.FromResult(new HttpObj());

        return Task.FromResult(new HttpObj { HttpDataObj = GetHttpDataObjFromQuery(item) });
    }

    private static HttpDataObj GetHttpDataObjFromQuery(ProcessItem item)
    {
        //nothing to do here, will set values from Query next step.
        var dataObj = Activator.CreateInstance(item.DataObjType!)!;
        return new HttpDataObj
        {
            StreamLength = 0,
            Value = dataObj,
            CallId = null,
            ConnId = null,
            Type = dataObj.GetType()
        };
    }
}

internal sealed class HttpObjProcessorManager
{
    private readonly List<IHttpObjProcessor> _processors;

    public HttpObjProcessorManager(IEnumerable<IHttpObjProcessor> processors)
    {
        _processors = processors.ToList();
    }

    public async Task<HttpObj> ProcessAsync(ProcessItem item)
    {
        foreach (var p in _processors)
            if (p.MatchContentType(item.HttpRequest.ContentType))
            {
                var obj = await p.ProcessAsync(item);

                //set path values
                obj.HttpDataObj.SetValues(item.HttpRoutInfo.MatchesPathValues(item.FormatRawPath));

                //set query values
                var valuesFromQuery = GetValuesFromQuery(item.HttpRequest, item.HttpRoutInfo.QueryParams, obj.ProxyStream != null);

                obj.HttpDataObj.SetValues(valuesFromQuery);

                return obj;
            }

        throw new HttpFailedException($"ContentType:'{item.HttpRequest.ContentType}' is not supported.");
    }

    private static Dictionary<string, StringValues> GetValuesFromQuery(HttpRequest request, Dictionary<string, string> queryParams, bool hasStream)
    {
        var ret = new Dictionary<string, StringValues>();
        List<KeyValuePair<string, StringValues>> pairs = new();

        pairs.AddRange(request.Query);

        //Form may be read by stream before.
        if (!hasStream && request.HasFormContentType)
            pairs.AddRange(request.Form);

        foreach (var p in pairs)
        {
            string pName;
            if (queryParams.TryGetValue(p.Key.ToLower(), out var outName))
                pName = outName;
            else
                pName = p.Key;

            ret.Add(pName, p.Value);
        }

        return ret;
    }
}

internal sealed class ProcessItem
{
    public ProcessItem(HttpRequest httpRequest, HttpRoutInfo httpRoutInfo, string formatRawPath, Type? dataObjType, Type? dataObjTypeWithoutPathQueryStream)
    {
        HttpRequest = httpRequest;
        HttpRoutInfo = httpRoutInfo;
        FormatRawPath = formatRawPath;
        DataObjType = dataObjType;
        DataObjTypeWithoutPathQueryStream = dataObjTypeWithoutPathQueryStream;
    }

    public HttpRequest HttpRequest { get; }
    public HttpRoutInfo HttpRoutInfo { get; }
    public string FormatRawPath { get; }

    public Type? DataObjType { get; }
    public Type? DataObjTypeWithoutPathQueryStream { get; }
}