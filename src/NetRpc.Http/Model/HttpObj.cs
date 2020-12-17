using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Primitives;
using NetRpc.Http.Client;

namespace NetRpc.Http
{
    internal sealed class HttpObj
    {
        public HttpDataObj HttpDataObj { get; set; } = new();

        public ProxyStream? ProxyStream { get; set; }
    }

    internal sealed class HttpDataObj
    {
        public string? ConnId { get; set; }

        public string? CallId { get; set; }

        public long StreamLength { get; set; }

        /// <summary>
        /// Is a InnerValue from MergeArgType
        /// </summary>
        public object? Value { get; set; }

        /// <summary>
        /// web api type. is a InnerType from MergeArgType
        /// </summary>
        public Type Type { get; set; } = null!;

        public bool TrySetStreamName(string streamName)
        {
            if (Value == null)
                return false;

            var f = Value.GetType().GetProperties().FirstOrDefault(i => i.Name.IsStreamName());
            if (f == null)
                return false;

            try
            {
                f.SetValue(Value, streamName);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void SetValues(Dictionary<string, string> keyValues)
        {
            Dictionary<string, StringValues> keyValues2 = new();
            foreach (var (key, value) in keyValues)
                keyValues2[key] = new StringValues(value);
            SetValues(keyValues2);
        }

        public void SetValues(Dictionary<string, StringValues> keyValues)
        {
            if (keyValues.Count == 0)
                return;

            if (keyValues.TryGetValue(CallConst.CallIdName, out var v1))
                CallId = v1;

            if (keyValues.TryGetValue(CallConst.ConnIdName, out var v2))
                ConnId = v2;

            CheckValue();

            var ps = Type.GetProperties();

            //set values
            foreach (var p in keyValues)
            {
                var f = ps.FirstOrDefault(i => string.Equals(i.GetJsonNameOrPropName(), p.Key, StringComparison.Ordinal));
                if (f != null)
                {
                    try
                    {
                        //may be need type convert
                        SetPropertyValue(Value!, f, ConvertValues(f.PropertyType, p.Value));
                    }
                    catch (Exception ex)
                    {
                        throw new HttpNotMatchedException($"{p.Key}:{p.Value}' is not valid value, {ex.Message}");
                    }
                }
            }
        }

        private void CheckValue()
        {
            //if null, create default.
            Value ??= Activator.CreateInstance(Type);

            //if inner obj null, create default.
            var ps = Type.GetProperties().ToList();
            if (ps.IsSingleCustomValue() && ps[0].GetValue(Value) == null)
                ps[0].SetValue(Value, Activator.CreateInstance(ps[0].PropertyType));
        }

        private static void SetPropertyValue(object classInstance, PropertyInfo tgtProperty, object? propertyValue)
        {
            var type = classInstance.GetType();

            if (tgtProperty.PropertyType.IsEnum)
                propertyValue = Enum.ToObject(tgtProperty.PropertyType, propertyValue!);

            if (SetBaseValue(classInstance, tgtProperty, propertyValue, type))
                return;

            if (propertyValue == DBNull.Value || propertyValue == null)
                type.InvokeMember(tgtProperty.Name, BindingFlags.SetProperty, Type.DefaultBinder, classInstance, new object[] {null!});
            else if (typeof(IConvertible).IsAssignableFrom(tgtProperty.PropertyType))
            {
                type.InvokeMember(tgtProperty.Name, BindingFlags.SetProperty, Type.DefaultBinder, classInstance,
                    new[] {Convert.ChangeType(propertyValue, tgtProperty.PropertyType)});
            }
            else if (tgtProperty.PropertyType == typeof(DateTimeOffset) ||
                     tgtProperty.PropertyType == typeof(DateTimeOffset?))
                tgtProperty.SetValue(classInstance, DateTimeOffset.Parse((string) propertyValue));
            else
                tgtProperty.SetValue(classInstance, propertyValue);
        }

        private static bool SetBaseValue(object classInstance, PropertyInfo tgtProperty, object? propertyValue, Type type)
        {
            var propertyValueStr = propertyValue as string;

            if (propertyValueStr == null)
                return false;

            if (tgtProperty.PropertyType == typeof(Guid))
            {
                type.InvokeMember(tgtProperty.Name, BindingFlags.SetProperty, Type.DefaultBinder, classInstance,
                    new[] {(object) Guid.Parse(propertyValueStr)});
                return true;
            }

            if (tgtProperty.PropertyType == typeof(bool?))
                tgtProperty.SetValue(classInstance, bool.Parse(propertyValueStr));
            else if (tgtProperty.PropertyType == typeof(int?))
                tgtProperty.SetValue(classInstance, int.Parse(propertyValueStr));
            else if (tgtProperty.PropertyType == typeof(uint?))
                tgtProperty.SetValue(classInstance, uint.Parse(propertyValueStr));
            else if (tgtProperty.PropertyType == typeof(long?))
                tgtProperty.SetValue(classInstance, long.Parse(propertyValueStr));
            else if (tgtProperty.PropertyType == typeof(ulong?))
                tgtProperty.SetValue(classInstance, ulong.Parse(propertyValueStr));
            else if (tgtProperty.PropertyType == typeof(short?))
                tgtProperty.SetValue(classInstance, short.Parse(propertyValueStr));
            else if (tgtProperty.PropertyType == typeof(ushort?))
                tgtProperty.SetValue(classInstance, ushort.Parse(propertyValueStr));
            else if (tgtProperty.PropertyType == typeof(float?))
                tgtProperty.SetValue(classInstance, float.Parse(propertyValueStr));
            else if (tgtProperty.PropertyType == typeof(double?))
                tgtProperty.SetValue(classInstance, double.Parse(propertyValueStr));
            else
                return false;

            return true;
        }

        private static object ConvertValues(Type t, StringValues sv)
        {
            if (typeof(IEnumerable).IsAssignableFrom(t) && t != typeof(string))
            {
                var json = sv.ToArray().ToDtoJson();
                return json.ToDtoObjectByNumber(t)!;
            }

            return sv[0];
        }
    }
}