using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NetRpc.Http
{
    internal sealed class HttpObj
    {
        public HttpDataObj HttpDataObj { get; set; } = new HttpDataObj();

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
                var f = ps.FirstOrDefault(i => string.Equals(i.Name, p.Key, StringComparison.CurrentCultureIgnoreCase));
                if (f != null)
                {
                    try
                    {
                        //may be need type convert
                        SetPropertyValue(Value!, f, p.Value);
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

        private static void SetPropertyValue(object classInstance, PropertyInfo tgtProperty, object propertyValue)
        {
            var type = classInstance.GetType();

            if (tgtProperty.PropertyType.IsEnum)
                propertyValue = Enum.ToObject(tgtProperty.PropertyType, propertyValue);

            if (propertyValue == DBNull.Value || propertyValue == null)
                type.InvokeMember(tgtProperty.Name, BindingFlags.SetProperty, Type.DefaultBinder, classInstance, new object[] {null!});
            else
                type.InvokeMember(tgtProperty.Name, BindingFlags.SetProperty, Type.DefaultBinder, classInstance,
                    new[] {Convert.ChangeType(propertyValue, tgtProperty.PropertyType)});
        }
    }
}