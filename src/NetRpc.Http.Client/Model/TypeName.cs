using System;

namespace NetRpc.Http.Client
{
    public class TypeName
    {
        public string Name { get; set; }
        public Type Type { get; set; }

        public TypeName(string name, Type type)
        {
            Name = name;
            Type = type;
        }

        public TypeName()
        {
        }
    }
}