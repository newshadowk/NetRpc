using System;

namespace NetRpc.Contract;

[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
public class SwaggerRoleAttribute : Attribute
{
    public string Role { get; }

    public SwaggerRoleAttribute(string role)
    {
        Role = role;
    }
}