using System;

namespace NetRpc
{
    public class TypeName
    {
        public string Name { get; }
        public Type Type { get; }

        public TypeName(string name, Type type)
        {
            Name = name;
            Type = type;
        }
    }
}