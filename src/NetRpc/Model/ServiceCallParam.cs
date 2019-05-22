using System;
using System.Collections.Generic;
using System.Threading;

namespace NetRpc
{
    [Serializable]
    public sealed class ServiceCallParam
    {
        public ActionInfo Action { get; }

        public object[] Args { get; }

        public Dictionary<string, object> Header { get; }

        public Action<object> Callback { get; }

        public CancellationToken Token { get; }

        public BufferBlockStream Stream { get; }

        public ServiceCallParam(OnceCallParam param, Action<object> callback, CancellationToken token, BufferBlockStream stream)
        {
            Action = param.Action;
            Args = param.Args;
            Callback = callback;
            Token = token;
            Stream = stream;
            Header = param.Header;
        }
    }
}