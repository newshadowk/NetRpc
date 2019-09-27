using System;
using System.Collections;
using System.Collections.Generic;
using OpenTracing.Propagation;

namespace NetRpc.OpenTracing
{
    internal sealed class RequestHeadersExtractAdapter : ITextMap
    {
        private readonly Dictionary<string, object> _headers;

        public RequestHeadersExtractAdapter(Dictionary<string, object> headers)
        {
            _headers = headers ?? throw new ArgumentNullException(nameof(headers));
        }

        public void Set(string key, string value)
        {
            throw new NotSupportedException("This class should only be used with ITracer.Extract");
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            foreach (var kvp in _headers)
            {
                yield return new KeyValuePair<string, string>(kvp.Key, kvp.Value.ToString());
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}