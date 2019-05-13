using System;
using System.Collections.Generic;
using System.Threading;

namespace Nrpc
{
    [Serializable]
    public sealed class ServiceCallParam
    {
        public MethodInfoDto Method { get; }

        public object[] Args { get; }

        public Dictionary<string, object> Header { get; }

        public Action<object> Callback { get; }

        public CancellationToken Token { get; }

        public BufferBlockStream Stream { get; }

        public ServiceCallParam(OnceCallParam param, Action<object> callback, CancellationToken token, BufferBlockStream stream)
        {
            Method = param.Method;
            Args = param.Args;
            Callback = callback;
            Token = token;
            Stream = stream;
            Header = param.Header;
        }
    }
}