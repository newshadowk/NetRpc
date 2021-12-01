using System;

namespace NetRpc.Contract;

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class JsonParamNameAttribute : Attribute
{
    public string Name { get; }

    public JsonParamNameAttribute(string name)
    {
        Name = name;
    }
}