using System;

namespace NetRpc.Contract
{
    [AttributeUsage(AttributeTargets.Interface)]
    public sealed class ServiceNameAttribute : Attribute
    {
        public ServiceNameAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}