using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NetRpc
{
    public class RpcContext
    {
        private object _result;

        public IServiceProvider ServiceProvider { get; }

        public Dictionary<string, object> Header { get; }

        public object Target { get; }

        public MethodInfo InstanceMethodInfo { get; }

        public MethodInfo InterfaceMethodInfo { get; }

        public object[] Args { get; }

        public ActionInfo ActionInfo { get; }

        public object Result
        {
            get => _result;
            set
            {
                if (value is Task)
                    throw new InvalidCastException("MiddlewareContext Result can not be a Task.");
                _result = value;
            }
        }

        public RpcContext(Dictionary<string, object> header, object target, MethodInfo instanceMethodInfo, MethodInfo interfaceMethodInfo, ActionInfo actionInfo, object[] args, IServiceProvider serviceProvider)
        {
            Header = header;
            Target = target;
            InstanceMethodInfo = instanceMethodInfo;
            Args = args;
            ServiceProvider = serviceProvider;
            InterfaceMethodInfo = interfaceMethodInfo;
            ActionInfo = actionInfo;
        }

        public override string ToString()
        {
            return $"Header:{DicToStringForDisplay(Header)}, MethodName:{InstanceMethodInfo.Name}, Args:{ListToStringForDisplay(Args, ",")}";
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