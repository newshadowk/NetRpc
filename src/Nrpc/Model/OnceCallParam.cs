using System;
using System.Collections.Generic;

namespace Nrpc
{
    [Serializable]
    public sealed class OnceCallParam
    {
        public MethodInfoDto Method { get; set; }

        public object[] Args { get; set; }

        public Dictionary<string, object> Header { get; set; }

        public OnceCallParam(Dictionary<string, object> header, MethodInfoDto method, object[] args)
        {
            Method = method;
            Args = args;
            Header = header;
        }

        public override string ToString()
        {
            return $"{Method}({Args.ListToString(", ")})";
        }
    }

    [Serializable]
    public sealed class MethodInfoDto
    {
        public string FullName { get; set; }

        public string[] GenericArguments { get; set; }

        public override string ToString()
        {
            return $"{FullName}<{GenericArguments.ListToString(",")}>";
        }
    }
}