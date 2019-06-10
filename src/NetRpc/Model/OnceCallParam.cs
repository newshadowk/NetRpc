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

        public Dictionary<string, object> Header { get; set; }

        public OnceCallParam(Dictionary<string, object> header, ActionInfo action, long? streamLength, object[] args)
        {
            Action = action;
            Args = args;
            Header = header;
            StreamLength = streamLength;
        }

        public override string ToString()
        {
            return $"{Action}({Args.ListToString(", ")})";
        }
    }
}