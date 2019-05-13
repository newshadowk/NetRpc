using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Nrpc
{
    public class ApiContext
    {
        public Dictionary<string, object> Header { get; }

        public object Target { get; }

        public MethodInfo Method { get; }

        public object[] Args { get; }

        public ApiContext(Dictionary<string, object> header, object target, MethodInfo method, object[] args)
        {
            Header = header;
            Target = target;
            Method = method;
            Args = args;
        }

        public override string ToString()
        {
            return $"Header:{DicToStringForDisplay(Header)}, MethodName:{Method.Name}, Args:{ListToStringForDisplay(Args, ",")}";
        }

        private static string ListToStringForDisplay(Array list, string split)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("[Count:" + list.Length + "]");
            sb.Append(split);

            foreach (var s in list)
            {
                sb.Append(s);
                sb.Append(split);
            }

            return sb.ToString().TrimEndString(split);
        }

        public static string DicToStringForDisplay(Dictionary<string, object> header)
        {
            string s = "";
            foreach (KeyValuePair<string, object> p in header)
                s += $"{p.Key}:{p.Value}, ";
            return s;
        }
    }
}