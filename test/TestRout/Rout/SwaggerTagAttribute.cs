using System;

namespace NetRpc
{
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, Inherited = false)]
    public class SwaggerTagAttribute : Attribute
    {
        public SwaggerTagAttribute(string tag)
        {
            Tag = tag;
        }

        public string Tag { get; }
    }
}