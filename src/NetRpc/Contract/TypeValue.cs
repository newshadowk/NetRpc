using System;

namespace NetRpc
{
    public class TypeValue
    {
        public Type Type { get; }
        public object? Value { get; }

        public TypeValue(Type type, object value)
        {
            Type = type;
            Value = value;
        }
    }
}