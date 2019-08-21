using System;

namespace NetRpc
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public sealed class NetRpcPostAttribute : Attribute
    {
    }
}