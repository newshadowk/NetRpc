using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace NetRpc
{
    public sealed class ContractMethod
    {
        public MethodInfo MethodInfo { get; }

        public ReadOnlyCollection<MethodParameter> Parameters { get; }

        public ReadOnlyCollection<MethodParameter> InnerSystemTypeParameters { get; }

        public ContractMethod(Type contractType, string contractTypeTag, MethodInfo methodInfo, List<MethodParameter> parameters,
            List<FaultExceptionAttribute> faultExceptionAttributes, List<HttpHeaderAttribute> httpHeaderAttributes,
            List<ResponseTextAttribute> responseTextAttributes, List<SecurityApiKeyAttribute> securityApiKeyAttributes)
        {
            MethodInfo = methodInfo;
            Parameters = new ReadOnlyCollection<MethodParameter>(parameters);
            InnerSystemTypeParameters = new ReadOnlyCollection<MethodParameter>(GetInnerSystemTypeParameters(parameters));
            FaultExceptionAttributes = new ReadOnlyCollection<FaultExceptionAttribute>(faultExceptionAttributes);
            HttpHeaderAttributes = new ReadOnlyCollection<HttpHeaderAttribute>(httpHeaderAttributes);
            ResponseTextAttributes = new ReadOnlyCollection<ResponseTextAttribute>(responseTextAttributes);
            SecurityApiKeyAttributes = new ReadOnlyCollection<SecurityApiKeyAttribute>(securityApiKeyAttributes);

            //IgnoreAttribute
            IsGrpcIgnore = GetCustomAttribute<GrpcIgnoreAttribute>(contractType, methodInfo) != null;
            IsRabbitMQIgnore = GetCustomAttribute<RabbitMQIgnoreAttribute>(contractType, methodInfo) != null;
            IsHttpIgnore = GetCustomAttribute<HttpIgnoreAttribute>(contractType, methodInfo) != null;
            IsTracerIgnore = GetCustomAttribute<TracerIgnoreAttribute>(contractType, methodInfo) != null;
            IsTracerArgsIgnore = GetCustomAttribute<TracerArgsIgnoreAttribute>(contractType, methodInfo) != null;
            IsTraceReturnIgnore = GetCustomAttribute<TracerReturnIgnoreAttribute>(contractType, methodInfo) != null;

            Route = new MethodRoute(contractType, methodInfo);
            MergeArgType = GetMergeArgType(methodInfo);
            IsMQPost = GetCustomAttribute<MQPostAttribute>(contractType, methodInfo) != null;

            Tags = new ReadOnlyCollection<string>(GetTags(contractTypeTag, methodInfo));
        }

        public MergeArgType MergeArgType { get; }

        public ReadOnlyCollection<string> Tags { get; }

        public MethodRoute Route { get; }

        public bool IsTracerArgsIgnore { get; }

        public bool IsTraceReturnIgnore { get; }

        public bool IsGrpcIgnore { get; }

        public bool IsRabbitMQIgnore { get; }

        public bool IsHttpIgnore { get; }

        public bool IsTracerIgnore { get; }

        public bool IsMQPost { get; }

        public ReadOnlyCollection<FaultExceptionAttribute> FaultExceptionAttributes { get; }

        public ReadOnlyCollection<HttpHeaderAttribute> HttpHeaderAttributes { get; }

        public ReadOnlyCollection<ResponseTextAttribute> ResponseTextAttributes { get; }

        public ReadOnlyCollection<SecurityApiKeyAttribute> SecurityApiKeyAttributes { get; }

        public bool IsSupportAllParameter()
        {
            return IsSupportAllParameter(Parameters);
        }

        private static List<MethodParameter> GetInnerSystemTypeParameters(IList<MethodParameter> ps)
        {
            if (ps.Count == 0)
                return new List<MethodParameter>();

            var ret = new List<MethodParameter>();
            if (ps.Count == 1 && !ps[0].Type.IsSystemType())
                ps = ps[0].Type.GetProperties().ToList().ConvertAll(i => new MethodParameter(i.Name, i.PropertyType));

            foreach (var p in ps)
            {
                if (p.Type.IsSystemType())
                    ret.Add(p);
            }

            return ret;
        }

        private static bool IsSupportAllParameter(IList<MethodParameter> ps)
        {
            if (ps.Count == 0)
                return false;

            if (ps.Count == 1 && !ps[0].Type.IsSystemType())
            {
                if (ps[0].Type.GetProperties().Any(i => !i.PropertyType.IsSystemType()))
                    return false;
                return true;
            }

            if (ps.Any(i => !i.Type.IsSystemType()))
                return false;
            return true;
        }

        private static MergeArgType GetMergeArgType(MethodInfo m)
        {
            string streamName = null;
            TypeName action = null;
            TypeName cancelToken = null;

            // ReSharper disable once PossibleNullReferenceException
            var typeName = $"{m.DeclaringType.Namespace}_{m.DeclaringType.Name}_{m.Name}Param2";
            var typeNameWithoutStreamName = $"{m.DeclaringType.Namespace}_{m.DeclaringType.Name}_{m.Name}Param";
            var cis = new List<CustomsPropertyInfo>();

            var attributeData = CustomAttributeData.GetCustomAttributes(m).Where(i => i.AttributeType == typeof(ExampleAttribute)).ToList();
            var addedCallId = false;
            var addedStream = false;
            foreach (var p in m.GetParameters())
            {
                //Stream
                if (p.ParameterType == typeof(Stream))
                {
                    streamName = p.Name;
                    addedStream = true;
                    continue;
                }

                //callback
                if (p.ParameterType.IsFuncT())
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

            //StreamLength
            if (addedStream)
                cis.Add(new CustomsPropertyInfo(typeof(long), CallConst.StreamLength));

            var t = TypeFactory.BuildType(typeName, cis);
            var t2 = BuildTypeWithoutStreamName(typeNameWithoutStreamName, cis);

            if (cis.Count == 0)
                return new MergeArgType(null, null, null, null, null);

            return new MergeArgType(t, t2, streamName, action, cancelToken);
        }

        public object CreateMergeArgTypeObj(string callId, string connectionId, long streamLength, object[] args)
        {
            if (MergeArgType.Type == null)
                return null;

            var instance = Activator.CreateInstance(MergeArgType.Type);
            var newArgs = args.ToList();

            //_connectionId _callId streamLength
            newArgs.Add(connectionId);
            newArgs.Add(callId);
            newArgs.Add(streamLength);

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

        private static Type BuildTypeWithoutStreamName(string typeName, List<CustomsPropertyInfo> cis)
        {
            var list = cis.ToList();
            list.RemoveAll(i => i.PropertyName.IsStreamName() && i.Type == typeof(string));
            return TypeFactory.BuildType(typeName, list);
        }

        private static List<string> GetTags(string contractTypeTag, MethodInfo methodInfo)
        {
            var ret = new List<string>();
            ret.Add(contractTypeTag);

            var tags = methodInfo.GetCustomAttributes<TagAttribute>(true);
            foreach (var t in tags) 
                ret.Add(t.Name);

            return ret;
        }
    }

    public sealed class ContractInfo
    {
        public ContractInfo(Type type)
        {
            Type = type;

            SecurityApiKeyDefineAttributes = new ReadOnlyCollection<SecurityApiKeyDefineAttribute>(
                type.GetCustomAttributes<SecurityApiKeyDefineAttribute>(true).ToList());
     
            var methodInfos = type.GetInterfaceMethods().ToList();
            var faultDic = GetItemsFromDefines<FaultExceptionAttribute, FaultExceptionDefineAttribute>(type, methodInfos,
                (i, define) => i.DetailType == define.DetailType);
            var apiKeysDic = GetItemsFromDefines<SecurityApiKeyAttribute, SecurityApiKeyDefineAttribute>(type, methodInfos,
                (i, define) => i.Key == define.Key);
            var httpHeaderDic = GetAttributes<HttpHeaderAttribute>(type, methodInfos);
            var responseTextDic = GetAttributes<ResponseTextAttribute>(type, methodInfos);
            var tag = GetTag(type);

            var methods = new List<ContractMethod>();
            foreach (var f in faultDic)
                methods.Add(new ContractMethod(
                    type, 
                    tag,
                    f.Key,
                    GetMethodParameters(f.Key),
                    f.Value,
                    httpHeaderDic[f.Key],
                    responseTextDic[f.Key],
                    apiKeysDic[f.Key]));

            Methods = new ReadOnlyCollection<ContractMethod>(methods);
            Tags = new ReadOnlyCollection<string>(GetTags(methods));
        }

        public Type Type { get; }

        public ReadOnlyCollection<SecurityApiKeyDefineAttribute> SecurityApiKeyDefineAttributes { get; }

        public ReadOnlyCollection<ContractMethod> Methods { get; }

        public ReadOnlyCollection<string> Tags { get; }

        private static List<MethodParameter> GetMethodParameters(MethodInfo methodInfo)
        {
            var ret = new List<MethodParameter>();
            foreach (var p in methodInfo.GetParameters())
            {
                if (p.ParameterType.IsFuncT() || p.ParameterType == typeof(Stream))
                    continue;
                ret.Add(new MethodParameter(p.Name, p.ParameterType));
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

        private static string GetTag(Type type)
        {
            var tags = type.GetCustomAttributes<TagAttribute>(true).ToList();
            if (tags.Count > 1)
                throw new InvalidOperationException("TagAttribute on Interface is not allow multiple.");

            if (tags.Count == 0)
                return type.Name;
            return tags[0].Name;
        }

        private static List<string> GetTags(List<ContractMethod> methods)
        {
            var ret = new List<string>();
            methods.ForEach(i => ret.AddRange(i.Tags));
            return ret.Distinct().ToList();
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