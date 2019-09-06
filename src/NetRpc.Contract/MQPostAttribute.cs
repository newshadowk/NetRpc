using System;

namespace NetRpc
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class MQPostAttribute : Attribute
    {
    }
}