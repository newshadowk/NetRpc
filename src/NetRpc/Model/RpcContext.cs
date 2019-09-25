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

        public IServiceProvider ServiceProvider { get; }

        public Dictionary<string, object> Header { get; }

        public object Target { get; }

        public string TraceId { get; }

        public MethodInfo InstanceMethodInfo { get; }

        public MethodInfo ContractMethodInfo { get; }

        public Contract Contract { get; }

        public Type CallbackType { get; }

        public Action<object> Callback
        {
            get
            {
                var found = Args?.FirstOrDefault(i =>
                {
                    if (i == null)
                        return false;

                    return i.GetType().IsActionT();
                });
                if (found == null)
                    return null;
                return ActionHelper.ConvertAction(found);
            }
            set
            {
                if (Args == null)
                    return;

                for (var i = 0; i < Args.Length; i++)
                {
                    if (Args[i] == null)
                        continue;

                    var t = Args[i].GetType();
                    if (t.IsActionT())
                    {
                        Args[i] = ActionHelper.ConvertAction(value, CallbackType);
                        return;
                    }
                }
            }
        }

        public CancellationToken Token { get; }

        public Stream Stream { get; }

        public object[] Args { get; }

        public ActionInfo ActionInfo { get; }

        public RpcContext(IServiceProvider serviceProvider, 
            Dictionary<string, object> header, 
            object target, 
            string traceId,
            MethodInfo instanceMethodInfo,
            MethodInfo contractMethodInfo, 
            object[] args, 
            ActionInfo actionInfo,
            Stream stream,
            Contract contract,
            Action<object> callback,
            CancellationToken token)
        {
            ServiceProvider = serviceProvider;
            Header = header;
            Target = target;
            TraceId = traceId;
            InstanceMethodInfo = instanceMethodInfo;
            ContractMethodInfo = contractMethodInfo;
            Args = args;
            CallbackType = GetActionType(args);
            ActionInfo = actionInfo;
            Callback = callback;
            Stream = stream;
            Contract = contract;
            Token = token;

            ResetProps();
        }

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

        private void ResetProps()
        {
            if (Args == null)
                return;

            for (var i = 0; i < Args.Length; i++)
            {
                if (Args[i] == null)
                    continue;

                if (Args[i].GetType().IsCancellationToken())
                    Args[i] = Token;

                if (Args[i].GetType().IsSubclassOf(typeof(Stream)))
                    Args[i] = Stream;
            }
        }

        public override string ToString()
        {
            return $"Header:{DicToStringForDisplay(Header)}, MethodName:{InstanceMethodInfo.Name}, Args:{ListToStringForDisplay(Args, ",")}";
        }

        private static Type GetActionType(object[] args)
        {
            foreach (var i in args)
            {
                if (i == null)
                    continue;

                var t = i.GetType();
                if (t.IsActionT())
                    return t.GetGenericArguments()[0];
            }

            return null;
        }

        private static string ListToStringForDisplay(Array list, string split)
        {
            var sb = new StringBuilder();

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
            var s = "";
            foreach (var p in header)
                s += $"{p.Key}:{p.Value}, ";
            return s;
        }
    }
}