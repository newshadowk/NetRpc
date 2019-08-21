using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetRpc
{
    public class RpcContext
    {
        private object _result;
        private CancellationToken _token;
        private Stream _stream;

        public IServiceProvider ServiceProvider { get; set; }

        public Dictionary<string, object> Header { get; set; }

        public object Target { get; set; }

        public MethodInfo InstanceMethodInfo { get; set; }

        public MethodInfo ContractMethodInfo { get; set; }

        public Action<object> Callback
        {
            get
            {
                var found = Args?.FirstOrDefault(i => i.GetType().IsActionT());
                if (found == null)
                    return null;
                return ActionHelper.ConvertAction(found);
            }
            set
            {
                if (Args == null)
                    return;

                for (int i = 0; i < Args.Length; i++)
                {
                    var t = Args[i].GetType();
                    if (t.IsActionT())
                    {
                        Args[i] = ActionHelper.ConvertAction(value, t.GetGenericArguments()[0]);
                        return;
                    }
                }
            }
        }

        public CancellationToken Token
        {
            get => _token;
            set
            {
                _token = value;
                ResetToken();
            }
        }

        public Stream Stream
        {
            get => _stream;
            set
            {
                _stream = value;
                ResetStream();
            }
        }

        public object[] Args { get; set; }

        public ActionInfo ActionInfo { get; set; }

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

        private void ResetToken()
        {
            if (Args == null)
                return;

            for (int i = 0; i < Args.Length; i++)
            {
                if (Args[i].GetType().IsCancellationToken())
                    Args[i] = Token;
            }
        }

        private void ResetStream()
        {
            if (Args == null)
                return;

            for (int i = 0; i < Args.Length; i++)
            {
                if (Args[i].GetType() == typeof(Stream))
                    Args[i] = Stream;
            }
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