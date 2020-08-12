using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NetRpc.Contract;

namespace NetRpc
{
    internal static class MergeArgTypeFactory
    {
        public static MergeArgType Create(MethodInfo m, IList<string> pathQueryParams)
        {
            string? streamName = null;
            TypeName? action = null;
            TypeName? cancelToken = null;

            // ReSharper disable once PossibleNullReferenceException
            var typeName = $"{m.DeclaringType!.Namespace}_{m.DeclaringType.Name}_{m.Name}Param2";
            var typeNameWithoutStreamName = $"{m.DeclaringType.Namespace}_{m.DeclaringType.Name}_{m.Name}Param";
            var cis = new List<CustomsPropertyInfo>();

            //firstLevelParams
            var (firstLevelParams, isSingleValue, singleValue) = InnerType.GetFirstLevelParams(m, pathQueryParams);

            var attributeData = CustomAttributeData.GetCustomAttributes(m).Where(i => i.AttributeType == typeof(ExampleAttribute)).ToList();
            var addedCallId = false;
            var addedStream = false;
            var hasCustomType = false;
            foreach (var p in firstLevelParams)
            {
                //Stream
                if (p.Type.IsStream())
                {
                    streamName = p.Name;
                    addedStream = true;
                    continue;
                }

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

                //Custom Type
                //ExampleAttribute
                var found = attributeData.Find(i => (string)i.ConstructorArguments[0].Value! == p.Name);
                if (found != null)
                    cis.Add(new CustomsPropertyInfo(p.Type, p.Name!, found));
                else
                    cis.Add(new CustomsPropertyInfo(p.Type, p.Name!));
                hasCustomType = true;
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
            var t2 = BuildTypeWithoutStreamName(typeNameWithoutStreamName, cis);

            if (cis.Count == 0)
                return new MergeArgType(null, null, null, null, null, false, false, null);

            return new MergeArgType(t, t2, streamName, action, cancelToken, hasCustomType, isSingleValue, singleValue);
        }

        private static Type BuildTypeWithoutStreamName(string typeName, List<CustomsPropertyInfo> cis)
        {
            var list = cis.ToList();
            list.RemoveAll(i => i.PropertyName.IsStreamName() && i.Type == typeof(string));
            return TypeFactory.BuildType(typeName, list);
        }
    }
}