using System;

namespace NetRpc.Contract
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class MQPostAttribute : Attribute
    {
    }
}