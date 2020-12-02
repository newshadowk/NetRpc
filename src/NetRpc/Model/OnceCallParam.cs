using System;
using System.Collections.Generic;

namespace NetRpc
{
    [Serializable]
    public sealed class OnceCallParam
    {
        public ActionInfo Action { get; set; }

        public object?[] PureArgs { get; set; }

        public bool HasStream { get; set; }

        public long StreamLength { get; set; }

        public byte[]? PostStream { get; set; }

        public Dictionary<string, object?> Header { get; set; }

        public OnceCallParam(Dictionary<string, object?> header, ActionInfo action, bool hasStream, byte[]? postStream, long streamLength, object?[] pureArgs)
        {
            HasStream = hasStream;
            Action = action;
            PureArgs = pureArgs;
            Header = header;
            StreamLength = streamLength;
            PostStream = postStream;
        }

        public override string ToString()
        {
            return $"{Action}({PureArgs.ListToString(", ")})";
        }
    }
}