using System;

namespace NetRpc.Contract
{
    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = true)]
    public class SecurityApiKeyDefineAttribute : Attribute
    {
        public string Key { get; }
        public string Name { get; }
        public string Description { get; }

        public SecurityApiKeyDefineAttribute(string key, string name, string description)
        {
            Key = key;
            Name = name;
            Description = description;
        }
    }

    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, Inherited = false)]
    public class SecurityApiKeyAttribute : Attribute
    {
        public string Key { get; }

        public string? Name { get; }

        public SecurityApiKeyAttribute(string key)
        {
            Key = key;
        }
    }
}