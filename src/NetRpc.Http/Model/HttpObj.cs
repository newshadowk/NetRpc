using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NetRpc.Http
{
    internal sealed class HttpObj
    {
        public HttpDataObj HttpDataObj { get; set; } = new HttpDataObj();

        public ProxyStream ProxyStream { get; set; }
    }

    internal sealed class HttpDataObj
    {
        public string ConnectionId { get; set; }

        public string CallId { get; set; }

        public long StreamLength { get; set; }

        public object Value { get; set; }

        public Type Type { get; set; }

        public void SetValue(Dictionary<string, string> keyValues)
        {
            var mps = new List<MethodParameter>();
            foreach (var p in Type.GetProperties()) 
                mps.Add(new MethodParameter(p.Name, p.PropertyType));

            var (instance, ps) = GetInnerSystemTypeParameters();
            
            //set values
            foreach (var p in keyValues)
            {
                var f = ps.FirstOrDefault(i => i.Name.ToLower() == p.Key.ToLower());
                if (f != null)
                {
                    //may be need type convert
                    SetPropertyValue(instance, f, p.Value);
                }
            }
        }

        private static void SetPropertyValue(object classInstance, PropertyInfo tgtProperty, object propertyValue)
        {
            var type = classInstance.GetType();

            if (tgtProperty.PropertyType.IsEnum)
                propertyValue = Enum.ToObject(tgtProperty.PropertyType, propertyValue);

            if (propertyValue == DBNull.Value || propertyValue == null)
            {
                type.InvokeMember(tgtProperty.Name, BindingFlags.SetProperty, Type.DefaultBinder, classInstance, new object[] { null });
            }
            else
            {
                type.InvokeMember(tgtProperty.Name, BindingFlags.SetProperty, Type.DefaultBinder, classInstance,
                    new[] { Convert.ChangeType(propertyValue, tgtProperty.PropertyType) });
            }
        }

        private (object instance, List<PropertyInfo> ps) GetInnerSystemTypeParameters()
        {
            var ps = Type.GetProperties().ToList();

            if (ps.Count == 0)
                return (Value, new List<PropertyInfo>());

            object instance = Value;
            var ret = new List<PropertyInfo>();
            if (ps.Count == 1 && !ps[0].PropertyType.IsSystemType())
            {
                instance = ps[0].GetValue(Value);
                ps = ps[0].PropertyType.GetProperties().ToList();
            }

            foreach (var p in ps)
            {
                if (p.PropertyType.IsSystemType())
                    ret.Add(p);
            }

            return (instance, ret);
        }

        public bool TrySetStreamName(string streamName)
        {
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
    }
}