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

        public object[] PureArgs { get; }

        public long? StreamLength { get; set; }

        public Dictionary<string, object> Header { get; }

        public Action<object> Callback { get; }

        public CancellationToken Token { get; }

        public Stream Stream { get; }

        public ServiceCallParam(OnceCallParam param, Action<object> callback, CancellationToken token, Stream stream)
        {
            Action = param.Action;
            PureArgs = param.PureArgs;
            StreamLength = param.StreamLength;
            Callback = callback;
            Token = token;
            Stream = stream;
            Header = param.Header;
        }
    }
}