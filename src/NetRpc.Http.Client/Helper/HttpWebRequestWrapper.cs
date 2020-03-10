using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using RestSharp.Extensions;

namespace NetRpc.Http.Client
{
    internal sealed class HttpWebRequestWrapper
    {
        private string _boundary;
        private byte[] _boundaryBytes;
        private readonly HttpWebRequest _request;
        private Stream _requestStream;

        public HttpWebRequestWrapper(string url, int timeOut)
        {
            _request = (HttpWebRequest)WebRequest.Create(url);
            _request.Timeout = timeOut;
            _request.Method = "POST";
            _request.KeepAlive = true;
            _request.Credentials = CredentialCache.DefaultCredentials;
            _request.Accept = "application/json";
        }

        public void AddHeader(string name, string value)
        {
            _request.Headers.Add(name, value);
        }

        public void SetToJsonBody()
        {
            _request.ContentType = "application/json";
        }

        public void SetToFileStream()
        {
            _boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");
            _boundaryBytes = Encoding.ASCII.GetBytes("\r\n--" + _boundary + "\r\n");
            _request.ContentType = "multipart/form-data; boundary=" + _boundary;
        }

        public async Task<Stream> GetRequestStreamAsync()
        {
            _requestStream = await _request.GetRequestStreamAsync();
            return _requestStream;
        }

        public Task<WebResponse> GetResponseAsync()
        {
            return _request.GetResponseAsync();
        }

        public async Task WriteParameterAsync(NameValueCollection nvc)
        {
            var formDataTemplate = "Content-Disposition: form-data; name=\"{0}\"\r\n\r\n{1}";
            foreach (string key in nvc.Keys)
            {
                await _requestStream.WriteAsync(_boundaryBytes, 0, _boundaryBytes.Length);
                var formItem = string.Format(formDataTemplate, key, nvc[key]);
                var formItemBytes = Encoding.UTF8.GetBytes(formItem);
                await _requestStream.WriteAsync(formItemBytes, 0, formItemBytes.Length);
            }

            await _requestStream.WriteAsync(_boundaryBytes, 0, _boundaryBytes.Length);
        }

        public async Task WriteStreamAsync(Stream fileStream, string fileStreamName)
        {
            //write stream header
            var headerTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: {2}\r\n\r\n";
            var header = string.Format(headerTemplate, fileStreamName, fileStreamName, MimeTypeMap.GetMimeType(Path.GetExtension(fileStreamName)));
            var headerBytes = Encoding.UTF8.GetBytes(header);
            await _requestStream.WriteAsync(headerBytes, 0, headerBytes.Length);

            //write stream body
            await fileStream.CopyToAsync(_requestStream);
            fileStream.Close();
        }

        public async Task WriteJsonBodyAsync(object obj)
        {
            var bytes = Encoding.UTF8.GetBytes(obj.ToDtoJson());
            await _requestStream.WriteAsync(bytes, 0, bytes.Length);
        }

        public async Task WriteEndBoundaryAsync()
        {
            var trailer = Encoding.ASCII.GetBytes("\r\n--" + _boundary + "--\r\n");
            await _requestStream.WriteAsync(trailer, 0, trailer.Length);
            _requestStream.Close();
        }
    }
}