using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace NetRpc
{
    [Serializable]
    public sealed class ServiceCallParam
    {
        public ActionInfo Action { get; }

        public object[] Args { get; }

        public long? StreamLength { get; set; }

        public string TraceId { get; }

        public Dictionary<string, object> Header { get; }

        public Action<object> Callback { get; }

        public CancellationToken Token { get; }

        public Stream Stream { get; }

        public ServiceCallParam(OnceCallParam param, Action<object> callback, CancellationToken token, Stream stream)
        {
            TraceId = param.TraceId;
            Action = param.Action;
            Args = param.Args;
            StreamLength = param.StreamLength;
            Callback = callback;
            Token = token;
            Stream = stream;
            Header = param.Header;
        }
    }
}