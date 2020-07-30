using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;

namespace NetRpc.Http
{
    internal interface IHttpObjProcessor
    {
        bool MatchContentType(string contentType);

        Task<HttpObj> ProcessAsync(ProcessItem item);
    }

    /// <summary>
    /// multipart/form-data
    /// </summary>
    internal sealed class FormDataHttpObjProcessor : IHttpObjProcessor
    {
        private readonly FormOptions _defaultFormOptions = new FormOptions();

        public bool MatchContentType(string contentType)
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
            var section = await reader.ReadNextSectionAsync();

            //body
            ValidateSection(section);
            var ms = new MemoryStream();
            await section.Body.CopyToAsync(ms);
            var body = Encoding.UTF8.GetString(ms.ToArray());
            var dataObj = Helper.ToHttpDataObj(body, item.DataObjType!);

            //stream
            section = await reader.ReadNextSectionAsync();
            ValidateSection(section);
            var fileName = GetFileName(section.ContentDisposition);
            if (fileName == null)
                throw new ArgumentNullException("", "File name is null.");
            dataObj.TrySetStreamName(fileName);
            var proxyStream = new ProxyStream(section.Body, dataObj.StreamLength);
            return new HttpObj {HttpDataObj = dataObj, ProxyStream = proxyStream};
        }

        private static string? Match(string src, string left, string right)
        {
            var r = Regex.Match(src, $"(?<={left}).+(?={right})");
            if (r.Captures.Count > 0)
                return r.Captures[0].Value;
            return null;
        }

        private static string? GetFileName(string contentDisposition)
        {
            //Content-Disposition: form-data; name="stream"; filename="t1.docx"
            return Match(contentDisposition, "filename=\"", "\"");
        }

        private static void ValidateSection(MultipartSection section)
        {
            var hasContentDispositionHeader =
                ContentDispositionHeaderValue.TryParse(
                    section.ContentDisposition, out _);

            if (!hasContentDispositionHeader)
                throw new HttpFailedException("Has not ContentDispositionHeader.");
        }
    }

    /// <summary>
    /// application/json
    /// </summary>
    internal sealed class JsonHttpObjProcessor : IHttpObjProcessor
    {
        public bool MatchContentType(string contentType)
        {
            return contentType == "application/json";
        }

        public async Task<HttpObj> ProcessAsync(ProcessItem item)
        {
            string body;
            using (var sr = new StreamReader(item.HttpRequest.Body, Encoding.UTF8))
                body = await sr.ReadToEndAsync();

            var dataObj = Helper.ToHttpDataObj(body, item.DataObjType!);
            return new HttpObj {HttpDataObj = dataObj};
        }
    }

    /// <summary>
    /// application/x-www-form-urlencoded
    /// </summary>
    internal sealed class FormUrlEncodedObjProcessor : IHttpObjProcessor
    {
        public bool MatchContentType(string contentType)
        {
            return contentType == "application/x-www-form-urlencoded" ||
                   string.IsNullOrWhiteSpace(contentType);
        }

        public Task<HttpObj> ProcessAsync(ProcessItem item)
        {
            return Task.FromResult(new HttpObj {HttpDataObj = GetHttpDataObjFromQuery(item.HttpRequest, item.DataObjType!)});
        }

        private static HttpDataObj GetHttpDataObjFromQuery(HttpRequest request, Type dataObjType)
        {
            var dataObj = GetDataObjFromQuery(request, dataObjType);

            return new HttpDataObj
            {
                StreamLength = 0,
                Value = dataObj,
                CallId = null,
                ConnectionId = null,
                Type = dataObj.GetType()
            };
        }

        private static object GetDataObjFromQuery(HttpRequest request, Type dataObjType)
        {
            var dataObj = Activator.CreateInstance(dataObjType)!;
            var ps = dataObjType.GetProperties();
            var targetObj = dataObj;

            // dataObj is CustomObj? get inside properties.
            if (ps.Length == 1 && !ps[0].PropertyType.IsSystemType())
            {
                targetObj = Activator.CreateInstance(ps[0].PropertyType)!;
                ps[0].SetValue(dataObj, targetObj);
            }

            SetDataObj(request, targetObj);

            return dataObj;
        }

        private static void SetDataObj(HttpRequest request, object dataObj)
        {
            var ps = dataObj.GetType().GetProperties();
            foreach (var p in ps)
            {
                if (request.Query.TryGetValue(p.Name, out var values) ||
                    request.HasFormContentType && request.Form.TryGetValue(p.Name, out values))
                {
                    try
                    {
                        if (p.PropertyType == typeof(string))
                        {
                            p.SetValue(dataObj, values[0]);
                            continue;
                        }

                        // ReSharper disable once PossibleNullReferenceException
                        var parsedValue = p.PropertyType.GetMethod("Parse", new[] {typeof(string)})!.Invoke(null, new object[] {values[0]});
                        p.SetValue(dataObj, parsedValue);
                    }
                    catch (Exception ex)
                    {
                        throw new HttpNotMatchedException($"http get, '{p.Name}' is not valid value, {ex.Message}");
                    }
                }
            }
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
            if (item.DataObjType == null)
                return new HttpObj();

            foreach (var p in _processors)
                if (p.MatchContentType(item.HttpRequest.ContentType))
                {
                    var obj = await p.ProcessAsync(item);

                    //set path values
                    obj.HttpDataObj.SetValue(item.HttpRoutInfo.MatchesPathValues(item.FormatRawPath));

                    return obj;
                }

            throw new HttpFailedException($"ContentType:'{item.HttpRequest.ContentType}' is not supported.");
        }
    }

    internal sealed class ProcessItem
    {
        public ProcessItem(HttpRequest httpRequest, HttpRoutInfo httpRoutInfo, string formatRawPath, Type? dataObjType)
        {
            HttpRequest = httpRequest;
            HttpRoutInfo = httpRoutInfo;
            FormatRawPath = formatRawPath;
            DataObjType = dataObjType;
        }

        public HttpRequest HttpRequest { get; }
        public HttpRoutInfo HttpRoutInfo { get; }
        public string FormatRawPath { get; }
        public Type? DataObjType { get; }
    }
}