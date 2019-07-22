using System;
using System.Collections.Generic;
using System.Reflection;
using Namotion.Reflection;

namespace NetRpc.Http
{
    internal static class DefaultValueExtension
    {
        public static void SetDefaultValue(this object obj)
        {
            List<Type> contextTypes = new List<Type>();
            var type = obj.GetType();
            contextTypes.Add(type);
            foreach (var p in GetPropertiesByGetSet(type))
                SetDefaultValue(contextTypes, obj, p);
        }

        private static void SetDefaultValue(List<Type> contextTypes, object obj, PropertyInfo p)
        {
            SetRawValue(obj, p);

            if (contextTypes.Exists(i => i.FullName == p.PropertyType.FullName))
            {
                SetLastValue(p.GetValue(obj));
                return;
            }

            if (!NetRpc.Helper.IsSystemType(p.PropertyType))
                contextTypes.Add(p.PropertyType);

            var pObj = p.GetValue(obj);
            foreach (var pi in GetPropertiesByGetSet(p.PropertyType))
            {
                SetDefaultValue(contextTypes, pObj, pi);
            }
        }

        private static void SetLastValue(object o)
        {
            foreach (var p in GetPropertiesByGetSet(o.GetType()))
            {
                SetRawValue(o, p);
            }
        }

        private static void SetRawValue(object obj, PropertyInfo p)
        {
            if (p.PropertyType == typeof(string))
            {
                p.SetValue(obj, "string");
                return;
            }

            if (p.PropertyType == typeof(DateTime) || p.PropertyType == typeof(DateTime?))
            {
                p.SetValue(obj, DateTime.Now);
            }

            if (!p.PropertyType.IsValueType && p.GetValue(obj) == null)
            {
                p.SetValue(obj, Activator.CreateInstance(p.PropertyType));
            }
        }

        private static List<PropertyInfo> GetPropertiesByGetSet(Type t)
        {
            var ret = new List<PropertyInfo>();
            foreach (var p in t.GetProperties())
            {
                if (p.CanRead && p.CanWrite)
                    ret.Add(p);
            }

            return ret;
        }
    }
}