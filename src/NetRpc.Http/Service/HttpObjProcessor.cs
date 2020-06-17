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

        Task<HttpObj> ProcessAsync(HttpRequest request, Type dataObjType);
    }

    /// <summary>
    /// multipart/form-data
    /// </summary>
    internal sealed class FormDataHttpObjProcessor : IHttpObjProcessor
    {
        private readonly FormOptions _defaultFormOptions = new FormOptions();

        public bool MatchContentType(string contentType)
        {
            return contentType == "multipart/form-data";
        }

        public async Task<HttpObj> ProcessAsync(HttpRequest request, Type dataObjType)
        {
            var boundary = MultipartRequestHelper.GetBoundary(
                MediaTypeHeaderValue.Parse(request.ContentType),
                _defaultFormOptions.MultipartBoundaryLengthLimit);
            var reader = new MultipartReader(boundary, request.Body);
            var section = await reader.ReadNextSectionAsync();

            //body
            ValidateSection(section);
            var ms = new MemoryStream();
            await section.Body.CopyToAsync(ms);
            var body = Encoding.UTF8.GetString(ms.ToArray());
            var dataObj = Helper.ToHttpDataObj(body, dataObjType);

            //stream
            section = await reader.ReadNextSectionAsync();
            ValidateSection(section);
            var fileName = GetFileName(section.ContentDisposition);
            dataObj.TrySetStreamName(fileName);
            var proxyStream = new ProxyStream(section.Body, dataObj.StreamLength);
            return new HttpObj {HttpDataObj = dataObj, ProxyStream = proxyStream};
        }

        private static string Match(string src, string left, string right)
        {
            var r = Regex.Match(src, $"(?<={left}).+(?={right})");
            if (r.Captures.Count > 0)
                return r.Captures[0].Value;
            return null;
        }

        private static string GetFileName(string contentDisposition)
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

        public async Task<HttpObj> ProcessAsync(HttpRequest request, Type dataObjType)
        {
            string body;
            using (var sr = new StreamReader(request.Body, Encoding.UTF8))
                body = await sr.ReadToEndAsync();

            var dataObj = Helper.ToHttpDataObj(body, dataObjType);
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

        public Task<HttpObj> ProcessAsync(HttpRequest request, Type dataObjType)
        {
            return Task.FromResult(new HttpObj {HttpDataObj = Helper.GetHttpDataObjFromQuery(request, dataObjType)});
        }
    }

    internal sealed class HttpObjProcessorManager
    {
        private readonly List<IHttpObjProcessor> _processors;

        public HttpObjProcessorManager(IEnumerable<IHttpObjProcessor> processors)
        {
            _processors = processors.ToList();
        }

        public async Task<HttpObj> ProcessAsync(HttpRequest request, Type dataObjType)
        {
            if (dataObjType == null)
                return new HttpObj();

            foreach (var p in _processors)
                if (p.MatchContentType(request.ContentType))
                    return await p.ProcessAsync(request, dataObjType);
            throw new HttpFailedException($"ContentType:'{request.ContentType}' is not supported.");
        }
    }
}