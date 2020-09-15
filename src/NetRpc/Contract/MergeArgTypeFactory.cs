using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using NetRpc.Contract;

namespace NetRpc
{
    public static class MergeArgTypeFactory
    {
        /// <summary>
        /// key:name value:nameIndex
        /// </summary>
        private static readonly Dictionary<string, int> TypeDic = new Dictionary<string, int>();

        public static readonly List<InnerTypeMapItem> InnerTypeMap = new List<InnerTypeMapItem>();

        private static string GetName(string name)
        {
            name += "_";
            if (TypeDic.TryGetValue(name, out var i))
            {
                i++;
                var newName = name + i;
                TypeDic[name] = i;
                return newName;
            }

            TypeDic[name] = 0;
            return name;
        }

        public static MergeArgType Create(MethodInfo method, List<string> pathQueryParams)
        {
            //GetFirstLevelParams
            var (firstLevelParams, isSingleValue, singleValue) = InnerType.GetFirstLevelParams(method);

            string? streamName = null;
            TypeName? action = null;
            TypeName? cancelToken = null;

            //paramName
            string typeNameWithoutStreamName;
            string typeName;
            if (isSingleValue)
            {
                typeNameWithoutStreamName = GetName(singleValue!.ParameterType.Name);
                typeName = GetName(singleValue!.ParameterType.Name);
            }
            else
            {
                typeNameWithoutStreamName = GetName($"{method.Name}Param");
                typeName = GetName($"{method.Name}Param");
            }

            //cis
            var cis = new List<CustomsPropertyInfo>();
            var attributeData = CustomAttributeData.GetCustomAttributes(method).Where(i => i.AttributeType == typeof(ExampleAttribute)).ToList();
            var addedCallId = false;
            var addedStream = false;
            var hasCustomType = false;
            foreach (var p in firstLevelParams)
            {
                //callback
                if (p.Type.IsFuncT())
                {
                    action = new TypeName(p.Name!, p.Type);
                    addedCallId = true;
                    continue;
                }

                //cancel
                if (p.Type.IsCancellationToken())
                {
                    cancelToken = new TypeName(p.Name!, p.Type);
                    addedCallId = true;
                    continue;
                }

                //hasCustomType
                hasCustomType = true;

                //Stream
                if (p.Type.IsStream())
                {
                    streamName = p.Name;
                    addedStream = true;
                    continue;
                }

                //Custom Type
                //ExampleAttribute
                var found = FindCustomAttributeData(attributeData, p);
                if (found != null)
                    cis.Add(new CustomsPropertyInfo(p.Type, p.Name!, found));
                else
                    cis.Add(new CustomsPropertyInfo(p.Type, p.Name!));
            }

            //connectionId callId
            if (addedCallId)
            {
                cis.Add(new CustomsPropertyInfo(typeof(string), CallConst.ConnIdName));
                cis.Add(new CustomsPropertyInfo(typeof(string), CallConst.CallIdName));
            }

            //StreamLength
            if (addedStream)
                cis.Add(new CustomsPropertyInfo(typeof(long), CallConst.StreamLength));

            var t = TypeFactory.BuildType(typeName, cis);
            var t2 = BuildTypeWithoutPathQueryStream(typeNameWithoutStreamName, cis, pathQueryParams);

            if (cis.Count == 0)
                return new MergeArgType(null, null, null, null, null, 
                    false, false, null, method);

            //SetInnerTypeMap
            SetInnerTypeMap(t2, isSingleValue, singleValue!);

            return new MergeArgType(t, t2, streamName, action, cancelToken, hasCustomType, isSingleValue, singleValue, method);
        }

        private static CustomAttributeData? FindCustomAttributeData(List<CustomAttributeData> methodsAd, PPInfo p)
        {
            var found = methodsAd.Find(i => (string)i.ConstructorArguments[0].Value! == p.Name);
            if (found != null)
                return found;

            if (p.ParameterInfo != null)
            {
                found = CustomAttributeData.GetCustomAttributes(p.ParameterInfo).FirstOrDefault(i => i.AttributeType == typeof(ExampleAttribute));
                if (found != null)
                    return found;
            }
            else
                return CustomAttributeData.GetCustomAttributes(p.PropertyInfo!).FirstOrDefault(i => i.AttributeType == typeof(ExampleAttribute));

            return null;
        }

        private static void SetInnerTypeMap(Type mergeArgType, bool isSingleValue, ParameterInfo singleValue)
        {
            if (isSingleValue)
            {
                InnerTypeMap.Add(new InnerTypeMapItem(singleValue!.ParameterType, mergeArgType));

                var ps1 = mergeArgType.GetProperties().ToList();
                foreach (var p0 in singleValue.ParameterType.GetProperties())
                {
                    var f = ps1.FirstOrDefault(i => i.Name == p0.Name);
                    if (f != null)
                        InnerTypeMap.Add(new InnerTypeMapItem(p0, f));
                }
            }
        }

        private static Type BuildTypeWithoutPathQueryStream(string typeName, List<CustomsPropertyInfo> cis, List<string> pathQueryParams)
        {
            var list = cis.ToList();
            list.RemoveAll(i => 
                i.PropertyName.IsStreamName() && i.Type == typeof(string) // stream
                ||
                !i.Type.IsFuncT() && !i.Type.IsCancellationToken() && pathQueryParams.Any(j => j == i.PropertyName.ToLower()) // pathQueryParams
                );

            return TypeFactory.BuildType(typeName, list);
        }
    }
}