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

    public sealed class ContractMethod
    {
        public MethodInfo MethodInfo { get; }

        public List<MethodParameter> Parameters { get; }

        public ContractMethod(Type contractType, MethodInfo methodInfo, List<MethodParameter> parameters, List<FaultExceptionAttribute> faultExceptionAttributes,
            List<HttpHeaderAttribute> httpHeaderAttributes, List<ResponseTextAttribute> responseTextAttributes, List<SecurityApiKeyAttribute> securityApiKeyAttributes)
        {
            MethodInfo = methodInfo;
            Parameters = parameters;
            FaultExceptionAttributes = faultExceptionAttributes;
            HttpHeaderAttributes = httpHeaderAttributes;
            ResponseTextAttributes = responseTextAttributes;
            SecurityApiKeyAttributes = securityApiKeyAttributes;
            HttpRoutInfo = GetHttpRoutInfo(contractType, methodInfo);
            TagAttributes = methodInfo.GetCustomAttributes<TagAttribute>(true).ToList();
            MergeArgType = GetMergeArgType(methodInfo);

            //IgnoreAttribute
            IsGrpcIgnore = GetCustomAttribute<GrpcIgnoreAttribute>(contractType, methodInfo) != null;
            IsRabbitMQIgnore = GetCustomAttribute<RabbitMQIgnoreAttribute>(contractType, methodInfo) != null;
            IsHttpIgnore = GetCustomAttribute<HttpIgnoreAttribute>(contractType, methodInfo) != null;
            IsTracerIgnore = GetCustomAttribute<TracerIgnoreAttribute>(contractType, methodInfo) != null;
            IsTracerArgsIgnore = GetCustomAttribute<TracerArgsIgnoreAttribute>(contractType, methodInfo) != null;
            IsTraceReturnIgnore = GetCustomAttribute<TracerReturnIgnoreAttribute>(contractType, methodInfo) != null;

            IsMQPost = GetCustomAttribute<MQPostAttribute>(contractType, methodInfo) != null;
        }

        public MergeArgType MergeArgType { get; }

        public HttpRoutInfo HttpRoutInfo { get; }

        public List<TagAttribute> TagAttributes { get; }

        public bool IsTracerArgsIgnore { get; }

        public bool IsTraceReturnIgnore { get; }

        public bool IsGrpcIgnore { get; }

        public bool IsRabbitMQIgnore { get; }

        public bool IsHttpIgnore { get; }

        public bool IsTracerIgnore { get; }

        public bool IsMQPost { get; }

        public List<FaultExceptionAttribute> FaultExceptionAttributes { get; }

        public List<HttpHeaderAttribute> HttpHeaderAttributes { get; }

        public List<ResponseTextAttribute> ResponseTextAttributes { get; }

        public List<SecurityApiKeyAttribute> SecurityApiKeyAttributes { get; }

        private static MergeArgType GetMergeArgType(MethodInfo m)
        {
            string streamName = null;
            TypeName action = null;
            TypeName cancelToken = null;

            // ReSharper disable once PossibleNullReferenceException
            var typeName = $"{m.DeclaringType.Namespace}_{m.DeclaringType.Name}_{m.Name}Param";
            var cis = new List<CustomsPropertyInfo>();

            var attributeData = CustomAttributeData.GetCustomAttributes(m).Where(i => i.AttributeType == typeof(ExampleAttribute)).ToList();
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

                //ExampleAttribute
                var found = attributeData.Find(i => (string) i.ConstructorArguments[0].Value == p.Name);
                if (found != null)
                    cis.Add(new CustomsPropertyInfo(p.ParameterType, p.Name, found));
                else
                    cis.Add(new CustomsPropertyInfo(p.ParameterType, p.Name));
            }

            //connectionId callId
            if (addedCallId)
            {
                cis.Add(new CustomsPropertyInfo(typeof(string), CallConst.ConnectionIdName));
                cis.Add(new CustomsPropertyInfo(typeof(string), CallConst.CallIdName));
            }
            
            var t = TypeFactory.BuildType(typeName, cis);
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

        private static T GetCustomAttribute<T>(Type contractType, MethodInfo methodInfo) where T : Attribute
        {
            var methodA = methodInfo.GetCustomAttribute<T>(true);
            if (methodA != null)
                return methodA;

            return contractType.GetCustomAttribute<T>(true);
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
        public ContractInfo(Type type)
        {
            Type = type;

            HttpRoute = type.GetCustomAttribute<HttpRouteAttribute>(true);
            SecurityApiKeyDefineAttributes = type.GetCustomAttributes<SecurityApiKeyDefineAttribute>(true).ToList();
            Methods = new List<ContractMethod>();

            var methodInfos = type.GetInterfaceMethods().ToList();
            var faultDic = GetItemsFromDefines<FaultExceptionAttribute, FaultExceptionDefineAttribute>(type, methodInfos, 
                (i, define) => i.DetailType == define.DetailType);
            var apiKeysDic = GetItemsFromDefines<SecurityApiKeyAttribute, SecurityApiKeyDefineAttribute>(type, methodInfos,
                (i, define) => i.Key == define.Key);
            var httpHeaderDic = GetAttributes<HttpHeaderAttribute>(type, methodInfos);
            var responseTextDic = GetAttributes<ResponseTextAttribute>(type, methodInfos);

            foreach (var f in faultDic) 
                Methods.Add(new ContractMethod(type, 
                    f.Key, 
                    GetMethodParameters(f.Key), 
                    f.Value, 
                    httpHeaderDic[f.Key],
                    responseTextDic[f.Key],
                    apiKeysDic[f.Key]));

            var tagDic = new Dictionary<string, TagAttribute>();
            Methods.ForEach(i => i.TagAttributes.ForEach(j => tagDic[j.Name] = j));
            TagAttributes = tagDic.Values.ToList();
        }

        public Type Type { get; }

        public List<SecurityApiKeyDefineAttribute> SecurityApiKeyDefineAttributes { get; }

        public List<ContractMethod> Methods { get; }

        public HttpRouteAttribute HttpRoute { get; }

        public List<TagAttribute> TagAttributes { get; }

        public string Route
        {
            get
            {
                if (HttpRoute?.Template == null)
                    return Type.Name;
                return HttpRoute.Template;
            }
        }

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

        private static Dictionary<MethodInfo, List<T>> GetItemsFromDefines<T, TDefine>(Type contractType, IEnumerable<MethodInfo> methodInfos, 
            Func<T, TDefine, bool> match)
            where T : Attribute
            where TDefine : Attribute
        {
            var dic = new Dictionary<MethodInfo, List<T>>();
            var defines = contractType.GetCustomAttributes<TDefine>(true).ToList();
            var items = contractType.GetCustomAttributes<T>(true).ToList();

            foreach (var m in methodInfos)
            {
                var tempItems = m.GetCustomAttributes<T>(true).ToList();
                tempItems.AddRange(items);
                foreach (var f in tempItems)
                {
                    var foundF = defines.FirstOrDefault(i => match(f, i));
                    if (foundF != null)
                        f.CopyPropertiesFrom(foundF);
                }

                dic[m] = tempItems;
            }

            return dic;
        }

        private static Dictionary<MethodInfo, List<T>> GetAttributes<T>(Type type, IEnumerable<MethodInfo> methodInfos) where T : Attribute
        {
            var dic = new Dictionary<MethodInfo, List<T>>();
            var typeAttrs = type.GetCustomAttributes<T>(true).ToList();
            foreach (var m in methodInfos)
            {
                var tempL = typeAttrs.ToList();
                tempL.AddRange(m.GetCustomAttributes<T>(true).ToList());
                dic[m] = tempL;
            }

            return dic;
        }
    }

    public class Contract
    {
        private readonly Func<IServiceProvider, object> _instanceFactory;

        public ContractInfo ContractInfo { get; }

        public Type InstanceType { get; private set; }

        public MethodInfo GetInstanceMethodInfo(string name, IServiceProvider serviceProvider)
        {
            if (InstanceType != null)
                return InstanceType.GetMethod(name);

            InstanceType = _instanceFactory(serviceProvider).GetType();
            return InstanceType.GetMethod(name);
        }

        public Contract(Type contractType, Type instanceType)
        {
            InstanceType = instanceType;
            ContractInfo = new ContractInfo(contractType);
        }

        public Contract(Type contractType, Func<IServiceProvider, object> instanceFactory)
        {
            _instanceFactory = instanceFactory;
            ContractInfo = new ContractInfo(contractType);
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