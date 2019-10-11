using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace NetRpc
{
    public static class CallConst
    {
        public const string ConnectionIdName = "_connectionId";
        public const string CallIdName = "_callId";
    }

    public class TypeName
    {
        public string Name { get; set; }
        public Type Type { get; set; }

        public TypeName(string name, Type type)
        {
            Name = name;
            Type = type;
        }

        public TypeName()
        {
        }
    }

    public sealed class MethodParameter
    {
        public int Index { get; }

        public string Name { get; }

        public MethodParameter(int index, string name)
        {
            Index = index;
            Name = name;
        }
    }

    public sealed class MergeArgType
    {
        public Type Type { get; }

        public string StreamName { get; }
        
        public TypeName CallbackAction { get; }

        public TypeName CancelToken { get; }

        public MergeArgType(Type type, string streamName, TypeName callbackAction, TypeName cancelToken)
        {
            Type = type;
            StreamName = streamName;
            CallbackAction = callbackAction;
            CancelToken = cancelToken;
        }
    }

    public sealed class MethodObj
    {
        public MethodInfo MethodInfo { get; }

        public List<MethodParameter> Parameters { get; }

        public MethodObj(MethodInfo methodInfo, List<MethodParameter> parameters)
        {
            MethodInfo = methodInfo;
            Parameters = parameters;
            MergeArgType = GetMergeArgType(methodInfo);
        }

        public MergeArgType MergeArgType { get; }

        private static MergeArgType GetMergeArgType(MethodInfo m)
        {
            string streamName = null;
            TypeName action = null;
            TypeName cancelToken = null;

            var typeName = $"{m.Name}Param";
            var t = ClassHelper.BuildType(typeName);
            var cis = new List<ClassHelper.CustomsPropertyInfo>();

            var addedCallId = false;
            foreach (var p in m.GetParameters())
            {
                if (p.ParameterType == typeof(Stream))
                {
                    streamName = p.Name;
                    continue;
                }

                //callback
                if (p.ParameterType.IsActionT())
                {
                    action = new TypeName
                    {
                        Type = p.ParameterType,
                        Name = p.Name
                    };

                    addedCallId = true;

                    continue;
                }

                //cancel
                if (p.ParameterType == typeof(CancellationToken?) || p.ParameterType == typeof(CancellationToken))
                {
                    cancelToken = new TypeName
                    {
                        Type = p.ParameterType,
                        Name = p.Name
                    };

                    addedCallId = true;

                    continue;
                }

                cis.Add(new ClassHelper.CustomsPropertyInfo(p.ParameterType, p.Name));
            }

            //connectionId callId
            if (addedCallId)
            {
                cis.Add(new ClassHelper.CustomsPropertyInfo(typeof(string), CallConst.ConnectionIdName));
                cis.Add(new ClassHelper.CustomsPropertyInfo(typeof(string), CallConst.CallIdName));
            }

            t = ClassHelper.AddProperty(t, cis);
            if (cis.Count == 0)
                return null;

            return new MergeArgType(t, streamName, action, cancelToken);
        }

        public object CreateMergeArgTypeObj(string callId, string connectionId, object[] args)
        {
            if (MergeArgType.Type == null)
                return null;

            var instance = Activator.CreateInstance(MergeArgType.Type);
            var newArgs = args.ToList();

            //_connectionId _callId
            newArgs.Add(connectionId);
            newArgs.Add(callId);

            var i = 0;
            foreach (var p in MergeArgType.Type.GetProperties())
            {
                p.SetValue(instance, newArgs[i]);
                i++;
            }

            return instance;
        }
    }

    public sealed class ContractInfo
    {
        private readonly Dictionary<MemberInfo, List<FaultExceptionAttribute>> _faultDic = new Dictionary<MemberInfo, List<FaultExceptionAttribute>>();

        public ContractInfo(Type type)
        {
            Type = type;

            var cDefines = type.GetCustomAttributes<FaultExceptionDefineAttribute>(true).ToList();
            var cFaults = type.GetCustomAttributes<FaultExceptionAttribute>(true).ToList();
            HttpRoute = type.GetCustomAttribute<HttpRouteAttribute>(true);

            foreach (var m in type.GetInterfaceMethods())
            {
                var faults = m.GetCustomAttributes<FaultExceptionAttribute>(true).ToList();
                faults.AddRange(cFaults);

                foreach (var f in faults)
                {
                    var foundF = cDefines.FirstOrDefault(i => i.DetailType == f.DetailType);
                    if (foundF != null)
                    {
                        f.StatusCode = foundF.StatusCode;
                        f.ErrorCode = foundF.ErrorCode;
                        f.Summary = foundF.Summary;
                    }
                }

                _faultDic[m] = faults;
                var ps = GetMethodParameters(m);
                MethodObjs.Add(new MethodObj(m, ps));
            }
        }

        public Type Type { get; }

        public List<FaultExceptionAttribute> GetFaults(MethodInfo contractMethod)
        {
            return _faultDic[contractMethod];
        }

        public List<MethodObj> MethodObjs { get; } = new List<MethodObj>();

        public HttpRouteAttribute HttpRoute { get; }

        private static List<MethodParameter> GetMethodParameters(MethodInfo methodInfo)
        {
            var ret = new List<MethodParameter>();
            int i = -1;
            foreach (var p in methodInfo.GetParameters())
            {
                i++;
                if (p.ParameterType.IsActionT() || p.ParameterType == typeof(Stream))
                    continue;

                ret.Add(new MethodParameter(i, p.Name));
            }

            return ret;
        }
    }

    public class Contract
    {
        private ContractInfo Info;

        private Type _contractType;

        public Type ContractType
        {
            get => _contractType;
            set
            {
                _contractType = value;
                Info = new ContractInfo(value);
            }
        }

        public List<FaultExceptionAttribute> GetFaults(MethodInfo contractMethod)
        {
            return Info.GetFaults(contractMethod);
        }

        public List<MethodObj> MethodObjs => Info.MethodObjs;

        public Type InstanceType { get; set; }

        public string Route
        {
            get
            {
                if (Info.HttpRoute == null)
                    return ContractType.Name;
                return Info.HttpRoute.Template;
            }
        }

        public Contract()
        {
        }

        public Contract(Type contractType, Type instanceType)
        {
            ContractType = contractType;
            InstanceType = instanceType;
        }
    }

    public sealed class Contract<TService, TImplementation> : Contract where TService : class
        where TImplementation : class, TService
    {
        public Contract()
        {
            ContractType = typeof(TService);
            InstanceType = typeof(TImplementation);
        }
    }
}