using System;

namespace NetRpc
{
    /// <summary>
    /// Interface AllowMultiple is false, Method is true.
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    public class TagAttribute : Attribute
    {
        public string Name { get; set; }

        public TagAttribute(string name)
        {
            Name = name;
        }
    }
}