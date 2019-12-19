using System;

namespace NetRpc.Http
{
    internal sealed class HttpDataObj
    {
        public string ConnectionId { get; set; }

        public string CallId { get; set; }

        public long StreamLength { get; set; }

        public object Value { get; set; }

        public Type Type { get; set; }
    }
}