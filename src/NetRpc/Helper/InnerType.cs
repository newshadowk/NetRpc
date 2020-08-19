using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace NetRpc
{
    public class XmlCommentsNodeNameHelper
    {
        public static string GetMemberNameForMethod(MethodInfo method)
        {
            var builder = new StringBuilder("M:");

            builder.Append(QualifiedNameFor(method.DeclaringType!));
            builder.Append($".{method.Name}");

            var parameters = method.GetParameters();
            if (parameters.Any())
            {
                var parametersNames = parameters.Select(p => p.ParameterType.IsGenericParameter
                    ? $"`{p.ParameterType.GenericParameterPosition}"
                    : QualifiedNameFor(p.ParameterType, expandGenericArgs: true));
                builder.Append($"({string.Join(",", parametersNames)})");
            }

            return builder.ToString();
        }

        public static string GetMemberNameForType(Type type)
        {
            var builder = new StringBuilder("T:");
            builder.Append(QualifiedNameFor(type));

            return builder.ToString();
        }

        public static string GetMemberNameForFieldOrProperty(MemberInfo fieldOrPropertyInfo)
        {
            var builder = new StringBuilder((fieldOrPropertyInfo.MemberType & MemberTypes.Field) != 0 ? "F:" : "P:");
            builder.Append(QualifiedNameFor(fieldOrPropertyInfo.DeclaringType!));
            builder.Append($".{fieldOrPropertyInfo.Name}");

            return builder.ToString();
        }

        private static string QualifiedNameFor(Type? type, bool expandGenericArgs = false)
        {
            if (type!.IsArray)
                return $"{QualifiedNameFor(type.GetElementType(), expandGenericArgs)}[]";

            var builder = new StringBuilder();

            if (!string.IsNullOrEmpty(type.Namespace))
                builder.Append($"{type.Namespace}.");

            if (type.IsNested)
            {
                builder.Append($"{string.Join(".", GetNestedTypeNames(type))}.");
            }

            if (type.IsConstructedGenericType && expandGenericArgs)
            {
                var nameSansGenericArgs = type.Name.Split('`').First();
                builder.Append(nameSansGenericArgs);

                var genericArgsNames = type.GetGenericArguments().Select(t =>
                {
                    return t.IsGenericParameter
                        ? $"`{t.GenericParameterPosition}"
                        : QualifiedNameFor(t, true);
                });

                builder.Append($"{{{string.Join(",", genericArgsNames)}}}");
            }
            else
            {
                builder.Append(type.Name);
            }

            return builder.ToString();
        }

        private static IEnumerable<string> GetNestedTypeNames(Type type)
        {
            if (!type.IsNested || type.DeclaringType == null) yield break;

            foreach (var nestedTypeName in GetNestedTypeNames(type.DeclaringType))
            {
                yield return nestedTypeName;
            }

            yield return type.DeclaringType.Name;
        }
    }

    public static class InnerType
    {
        public static bool IsSingleCustomValue(this IList<PropertyInfo> ps)
        {
            return ps.Count == 1 && !ps[0].PropertyType.IsSystemType();
        }

        public static (bool isSingleValue, ParameterInfo? singleValue) IsSingleCustomValue(this IList<ParameterInfo> ps)
        {
            var l = ps.ToList();
            l.RemoveAll(i => i.ParameterType.IsFuncT() || i.ParameterType.IsCancellationToken() || i.ParameterType.IsStream());
            var ret = l.Count == 1 && !l[0].ParameterType.IsSystemType();
            ParameterInfo? singleValue = null;
            if (ret)
                singleValue = l[0];
            return (ret, singleValue);
        }

        public static (List<PPInfo> ps, bool isSingleValue, ParameterInfo? singleValue) GetFirstLevelParams(MethodInfo method)
        {
            var mps = method.GetParameters();
            if (mps.Length == 0)
                return (new List<PPInfo>(), false, null);

            List<PPInfo> ret;

            //M1(Obj obj, [Func cb], [CancelToken token])
            var (isSingleValue, singleValue) = mps.IsSingleCustomValue();
            if (isSingleValue)
            {
                var ps = singleValue!.ParameterType.GetProperties();
                ret = ps.ToList().ConvertAll(i => new PPInfo(i));

                //Stream
                var f = mps.FirstOrDefault(i => i.ParameterType.IsStream());
                if (f != null)
                    ret.Add(new PPInfo(f));

                //cb
                f = mps.FirstOrDefault(i => i.ParameterType.IsFuncT());
                if (f != null)
                    ret.Add(new PPInfo(f));

                //cancel token
                f = mps.FirstOrDefault(i => i.ParameterType.IsCancellationToken());
                if (f != null)
                    ret.Add(new PPInfo(f));
            }
            else
                ret = mps.ToList().ConvertAll(i => new PPInfo(i));

            return (ret, isSingleValue, singleValue);
        }

        public static List<PPInfo> GetInnerTypeNames(List<PPInfo> ps)
        {
            var ret = new List<PPInfo>();
            var l = ps;

            var func = ps.FirstOrDefault(i => i.Type.IsFuncT());
            var cancel = ps.FirstOrDefault(i => i.Type.IsCancellationToken());
            var stream = ps.FirstOrDefault(i => i.Type.IsStream());

            l.RemoveAll(i => i.Type.IsFuncT() || i.Type.IsCancellationToken() || i.Type.IsStream());

            var isSingleValue = l.Count == 1 && !l[0].Type.IsSystemType();

            if (isSingleValue)
                l = l[0].Type.GetProperties().ToList().ConvertAll(i => new PPInfo(i));
            
            ret.AddRange(l);
            if (func != null)
                ret.Add(func);
            if (cancel != null)
                ret.Add(cancel);
            if (stream != null)
                ret.Add(stream);

            return ret;
        }

        public static List<PPInfo> GetInnerSystemTypeParameters(MethodInfo methodInfo)
        {
            //get raw typeNames
            var typeInfos = new List<PPInfo>();
            foreach (var p in methodInfo.GetParameters())
                typeInfos.Add(new PPInfo(p));

            if (typeInfos.Count == 0)
                return new List<PPInfo>();

            //get inner typeNames
            var ret = new List<PPInfo>();
            typeInfos = GetInnerTypeNames(typeInfos);

            bool idAdded = false;
            foreach (var p in typeInfos)
            {
                if (p.Type.IsStream())
                    continue;

                if (p.Type.IsFuncT() || p.Type.IsCancellationToken())
                {
                    if (!idAdded)
                    {
                        idAdded = true;
                        ret.Add(new PPInfo(CallConst.CallIdName, typeof(string)));
                        ret.Add(new PPInfo(CallConst.ConnIdName, typeof(string)));
                    }
                    continue;
                }

                if (p.Type.IsSystemType())
                    ret.Add(p);
            }

            return ret;
        }

        public static object?[] GetInnerPureArgs(object?[] pureArgs, HttpRoutInfo hri)
        {
            //class CusObj {P1, P2}
            //in:CusObj -> P1, P2  (need convert)
            //in:CusObj, P1 -> CusObj, P1 (no change)
            List<object?> retArgs = new List<object?>();
            if (hri.MergeArgType.IsSingleValue)
            {
                var inst = pureArgs[0];
                foreach (var p in hri.MergeArgType.SingleValue!.ParameterType.GetProperties()) 
                    retArgs.Add(Helper.GetPropertyValue(inst, p));
                return retArgs.ToArray();
            }

            return pureArgs;
        }
    }
}