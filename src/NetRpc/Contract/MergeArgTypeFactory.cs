using System;
using System.Collections.Generic;
using System.IO;
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
            var firstLevelParams = GetFirstLevelParams(m, pathQueryParams);

            var attributeData = CustomAttributeData.GetCustomAttributes(m).Where(i => i.AttributeType == typeof(ExampleAttribute)).ToList();
            var addedCallId = false;
            var addedStream = false;
            var hasCustomType = false;
            foreach (var p in firstLevelParams)
            {
                //Stream
                if (p.Type == typeof(Stream))
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
                return new MergeArgType(null, null, null, null, null, false);

            return new MergeArgType(t, t2, streamName, action, cancelToken, hasCustomType);
        }

        private static List<TypeName> GetFirstLevelParams(MethodInfo m, IList<string> pathQueryParams)
        {
            var mps = m.GetParameters();
            if (mps.Length == 0)
                return new List<TypeName>();

            List<TypeName> ret;

            //M1(Obj obj, [Func cb], [CancelToken token])
            var (isSingleValue, singleValue) = mps.IsSingleCustomValue();
            if (isSingleValue)
            {
                var ps = singleValue!.ParameterType.GetProperties();
                ret = ps.ToList().ConvertAll(i => new TypeName(i.Name, i.PropertyType));

                //cb
                var f = mps.FirstOrDefault(i =>i.ParameterType.IsFuncT());
                if (f != null)
                    ret.Add(new TypeName(f.Name, f.ParameterType));

                //cancel token
                f = mps.FirstOrDefault(i => i.ParameterType.IsCancellationToken());
                if (f != null)
                    ret.Add(new TypeName(f.Name, f.ParameterType));
            }
            else
                ret = mps.ToList().ConvertAll(i => new TypeName(i.Name, i.ParameterType));

            //filter by pathQueryParams
            ret.RemoveAll(i => 
                !i.Type.IsFuncT() && 
                !i.Type.IsCancellationToken() && 
                pathQueryParams.Any(j => j == i.Name.ToLower()));

            return ret;
        }

        private static Type BuildTypeWithoutStreamName(string typeName, List<CustomsPropertyInfo> cis)
        {
            var list = cis.ToList();
            list.RemoveAll(i => i.PropertyName.IsStreamName() && i.Type == typeof(string));
            return TypeFactory.BuildType(typeName, list);
        }
    }
}