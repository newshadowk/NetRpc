using System;

namespace NetRpc
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public class TagAttribute : Attribute
    {
        public string Name { get; set; }

        public TagAttribute(string name)
        {
            Name = name;
        }
    }
}