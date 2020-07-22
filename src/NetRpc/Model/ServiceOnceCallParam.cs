using System;
using System.Collections.Generic;

namespace NetRpc
{
    [Serializable]
    public sealed class ServiceOnceCallParam
    {
        public ActionInfo Action { get; set; }

        public object?[] PureArgs { get; set; }

        public long StreamLength { get; set; }

        public ReadStream? Stream { get; set; }

        public Dictionary<string, object?> Header { get; set; }

        public ServiceOnceCallParam(ActionInfo action, object?[] pureArgs, long streamLength, ReadStream? stream, Dictionary<string, object?> header)
        {
            Action = action;
            PureArgs = pureArgs;
            StreamLength = streamLength;
            Stream = stream;
            Header = header;
        }

        public override string ToString()
        {
            return $"{Action}({PureArgs.ListToString(", ")})";
        }
    }
}