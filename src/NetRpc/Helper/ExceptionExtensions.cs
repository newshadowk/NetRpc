using System;
using System.Reflection;
using System.Runtime.Serialization;

namespace NetRpc
{
    public static class ExceptionExtensions
    {
        public static void GetObjectData(this Exception e, SerializationInfo info)
        {
            foreach (PropertyInfo p in e.GetType().GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance))
                info.AddValue(p.Name, p.GetValue(e).ToBytes());
        }

        public static void SetObjectData(this Exception e, SerializationInfo info)
        {
            foreach (PropertyInfo p in e.GetType().GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance))
            {
                var bytes = (byte[])info.GetValue(p.Name, typeof(byte[]));
                var value = bytes.ToObject();
                p.SetValue(e, value);
            }
        }
    }
}