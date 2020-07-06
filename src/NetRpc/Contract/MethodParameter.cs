using System;

namespace NetRpc
{
    public sealed class MethodParameter
    {
        public string Name { get; }

        public Type Type { get; }

        public MethodParameter(string name, Type type)
        {
            Name = name;
            Type = type;
        }
    }
}