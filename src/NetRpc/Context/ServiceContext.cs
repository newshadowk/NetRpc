using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace NetRpc
{
    public sealed class ServiceContext
    {
        private object _result;

        public ChannelType ChannelType { get; }

        public IServiceProvider ServiceProvider { get; }

        public Dictionary<string, object> Header { get; }

        public object Target { get; }

        public MethodInfo InstanceMethodInfo { get; }

        public MethodInfo ContractMethodInfo { get; }

        public MethodObj MethodObj { get; }

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

        /// <summary>
        /// Args of invoked action without stream and action.
        /// </summary>
        public object[] PureArgs { get; }

        public ActionInfo ActionInfo { get; }

        /// <summary>
        /// A central location for sharing state between components during the invoking process.
        /// </summary>
        public Dictionary<object, object> Properties { get; set; } = new Dictionary<object, object>();

        public ServiceContext(IServiceProvider serviceProvider, 
            Dictionary<string, object> header, 
            object target, 
            MethodInfo instanceMethodInfo,
            MethodInfo contractMethodInfo, 
            object[] args, 
            object[] pureArgs, 
            ActionInfo actionInfo,
            Stream stream,
            Contract contract,
            ChannelType channelType,
            Action<object> callback,
            CancellationToken token)
        {
            ServiceProvider = serviceProvider;
            ChannelType = channelType;
            Header = header;
            Target = target;
            InstanceMethodInfo = instanceMethodInfo;
            ContractMethodInfo = contractMethodInfo;
            Args = args;
            PureArgs = pureArgs;
            CallbackType = GetActionType(args);
            ActionInfo = actionInfo;
            Callback = callback;
            Stream = stream;
            Contract = contract;
            Token = token;
            MethodObj = Contract.MethodObjs.Find(i => i.MethodInfo == contractMethodInfo);

            ResetProps();
        }

        /// <summary>
        /// Result of invoked action.
        /// </summary>
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
            return $"Header:{DicToStringForDisplay(Header)}, MethodName:{InstanceMethodInfo.Name}, Args:{Helper.ListToStringForDisplay(Args, ",")}";
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

        public static string DicToStringForDisplay(Dictionary<string, object> header)
        {
            var s = "";
            foreach (var p in header)
                s += $"{p.Key}:{p.Value}, ";
            return s;
        }
    }
}