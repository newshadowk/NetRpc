using System;

namespace NetRpc.Contract;

[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
public class HttpHeaderAttribute : Attribute
{
    public HttpHeaderAttribute(string name, string? description = null)
    {
        Description = description;
        Name = name;
    }

    public string Name { get; }

    public string? Description { get; }
}