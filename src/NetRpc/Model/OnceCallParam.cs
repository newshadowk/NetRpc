using System;
using System.Collections.Generic;

namespace NetRpc
{
    [Serializable]
    public sealed class OnceCallParam
    {
        public ActionInfo Action { get; set; }

        public object[] Args { get; set; }

        public long? StreamLength { get; set; }

        public string TraceId { get; }

        public byte[] PostStream { get; set; }

        public Dictionary<string, object> Header { get; set; }

        public OnceCallParam(string traceId, Dictionary<string, object> header, ActionInfo action, byte[] postStream, long? streamLength, object[] args)
        {
            TraceId = traceId;
            Action = action;
            Args = args;
            Header = header;
            StreamLength = streamLength;
            PostStream = postStream;
        }

        public override string ToString()
        {
            return $"{Action}({Args.ListToString(", ")})";
        }
    }
}