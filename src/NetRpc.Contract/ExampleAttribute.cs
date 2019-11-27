using System;

namespace NetRpc
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = true)]
    public class ExampleAttribute : Attribute
    {
        public string Key { get; }
        public object Value { get; }

        public ExampleAttribute(object value)
        {
            Value = value;
        }

        public ExampleAttribute(string key, object value)
        {
            Key = key;
            Value = value;
        }
    }
}