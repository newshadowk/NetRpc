using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace NetRpc
{
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

    public sealed class HttpRoutInfo
    {
        public string ContractPath { get; }

        public string MethodPath { get; }

        public HttpRoutInfo(string contractPath, string methodPath)
        {
            ContractPath = contractPath;
            MethodPath = methodPath;
        }

        public override string ToString()
        {
            return $"{ContractPath}/{MethodPath}";
        }
    }

    public sealed class MethodObj
    {
        public MethodInfo MethodInfo { get; }

        public List<MethodParameter> Parameters { get; }

        public MethodObj(Type contractType, MethodInfo methodInfo, List<MethodParameter> parameters)
        {
            MethodInfo = methodInfo;
            Parameters = parameters;
            MergeArgType = GetMergeArgType(methodInfo);
            HttpRoutInfo = GetHttpRoutInfo(contractType, methodInfo);

            //IgnoreAttribute
            GrpcIgnore = methodInfo.GetCustomAttribute<GrpcIgnoreAttribute>(true) != null;
            RabbitMQIgnore = methodInfo.GetCustomAttribute<RabbitMQIgnoreAttribute>(true) != null;
            HttpIgnore = methodInfo.GetCustomAttribute<HttpIgnoreAttribute>(true) != null;
        }

        public MergeArgType MergeArgType { get; }

        public HttpRoutInfo HttpRoutInfo { get; }

        public bool GrpcIgnore { get; }

        public bool RabbitMQIgnore { get; }

        public bool HttpIgnore { get; }

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
                return new MergeArgType(null, null, null, null);

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

        private static HttpRoutInfo GetHttpRoutInfo(Type contractType, MethodInfo methodInfo)
        {
            //contractPath
            string contractPath;
            var contractRoute = contractType.GetCustomAttribute<HttpRouteAttribute>(true);
            if (contractRoute?.Template == null)
                contractPath = contractType.Name;
            else
                contractPath = contractRoute.Template;

            //methodPath
            string methodPath;
            var methodRoute = methodInfo.GetCustomAttribute<HttpRouteAttribute>(true);
            if (methodRoute?.Template != null)
            {
                if (methodRoute.Template != null)
                {
                    methodPath = methodRoute.Template;
                }
                else
                {
                    methodPath = methodInfo.Name;
                    if (methodRoute.TrimActionAsync)
                        methodPath = methodPath.TrimEndString("Async");
                }
            }
            else
            {
                methodPath = methodInfo.Name;
                if (contractRoute != null && contractRoute.TrimActionAsync)
                    methodPath = methodPath.TrimEndString("Async");
            }

            return new HttpRoutInfo(contractPath, methodPath);
        }
    }

    public sealed class ContractInfo
    {
        private readonly Dictionary<MemberInfo, List<FaultExceptionAttribute>> _faultDic = new Dictionary<MemberInfo, List<FaultExceptionAttribute>>();
        private readonly Dictionary<MemberInfo, List<HttpHeaderAttribute>> _headerDic = new Dictionary<MemberInfo, List<HttpHeaderAttribute>>();
        
        public ContractInfo(Type type)
        {
            Type = type;

            //_faultDic
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
                        f.Description = foundF.Description;
                    }
                }

                _faultDic[m] = faults;
                var ps = GetMethodParameters(m);
                MethodObjs.Add(new MethodObj(type, m, ps));
            }

            //_headerDic
            var cHeaders = type.GetCustomAttributes<HttpHeaderAttribute>(true).ToList();
            foreach (var m in type.GetInterfaceMethods())
            {
                var tempH = cHeaders.ToList();
                var headers = m.GetCustomAttributes<HttpHeaderAttribute>(true).ToList();
                tempH.AddRange(headers);
                _headerDic[m] = tempH;
            }
        }

        public Type Type { get; }

        public List<FaultExceptionAttribute> GetFaults(MethodInfo contractMethod)
        {
            return _faultDic[contractMethod];
        }

        public List<HttpHeaderAttribute> GetHeaders(MethodInfo contractMethod)
        {
            return _headerDic[contractMethod];
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
        public ContractInfo ContractInfo { get; }

        public Type ContractType { get; }

        public List<MethodObj> MethodObjs => ContractInfo.MethodObjs;

        public Type InstanceType { get; }

        public string Route
        {
            get
            {
                if (ContractInfo.HttpRoute?.Template == null)
                    return ContractType.Name;
                return ContractInfo.HttpRoute.Template;
            }
        }

        public Contract(Type contractType, Type instanceType)
        {
            ContractType = contractType;
            InstanceType = instanceType;
            ContractInfo = new ContractInfo(ContractType);
        }
    }

    public sealed class Contract<TService, TImplementation> : Contract where TService : class
        where TImplementation : class, TService
    {
        public Contract() : base(typeof(TService), typeof(TImplementation))
        {
        }
    }
}