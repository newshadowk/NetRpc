using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NetRpc
{
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

        public static (List<TypeName> ps, bool isSingleValue, ParameterInfo? singleValue) GetFirstLevelParams(MethodInfo m, IList<string> pathQueryParams)
        {
            var mps = m.GetParameters();
            if (mps.Length == 0)
                return (new List<TypeName>(), false, null);

            List<TypeName> ret;

            //M1(Obj obj, [Func cb], [CancelToken token])
            var (isSingleValue, singleValue) = mps.IsSingleCustomValue();
            if (isSingleValue)
            {
                var ps = singleValue!.ParameterType.GetProperties();
                ret = ps.ToList().ConvertAll(i => new TypeName(i.Name, i.PropertyType));

                //Stream
                var f = mps.FirstOrDefault(i => i.ParameterType.IsStream());
                if (f != null)
                    ret.Add(new TypeName(f.Name, f.ParameterType));

                //cb
                f = mps.FirstOrDefault(i => i.ParameterType.IsFuncT());
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

            return (ret, isSingleValue, singleValue);
        }

        public static List<TypeName> GetInnerTypeNames(List<TypeName> ps)
        {
            var ret = new List<TypeName>();
            var l = ps;

            var func = ps.FirstOrDefault(i => i.Type.IsFuncT());
            var cancel = ps.FirstOrDefault(i => i.Type.IsCancellationToken());
            var stream = ps.FirstOrDefault(i => i.Type.IsStream());

            l.RemoveAll(i => i.Type.IsFuncT() || i.Type.IsCancellationToken() || i.Type.IsStream());

            var isSingleValue = l.Count == 1 && !l[0].Type.IsSystemType();

            if (isSingleValue)
                l = l[0].Type.GetProperties().ToList().ConvertAll(i => new TypeName(i.Name, i.PropertyType));

            ret.AddRange(l);
            if (func != null)
                ret.Add(func);
            if (cancel != null)
                ret.Add(cancel);
            if (stream != null)
                ret.Add(stream);

            return ret;
        }

        public static List<TypeName> GetInnerSystemTypeParameters(MethodInfo methodInfo)
        {
            //get raw typeNames
            var typeNames = new List<TypeName>();
            foreach (var p in methodInfo.GetParameters())
                typeNames.Add(new TypeName(p.Name!, p.ParameterType));

            if (typeNames.Count == 0)
                return new List<TypeName>();

            //get inner typeNames
            var ret = new List<TypeName>();
            typeNames = GetInnerTypeNames(typeNames);

            bool idAdded = false;
            foreach (var p in typeNames)
            {
                if (p.Type.IsStream())
                    continue;

                if (p.Type.IsFuncT() || p.Type.IsCancellationToken())
                {
                    if (!idAdded)
                    {
                        idAdded = true;
                        ret.Add(new TypeName(CallConst.CallIdName, typeof(string)));
                        ret.Add(new TypeName(CallConst.ConnIdName, typeof(string)));
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