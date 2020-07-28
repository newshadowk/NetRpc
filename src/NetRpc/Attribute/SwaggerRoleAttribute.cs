using System;

namespace NetRpc
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    public class SwaggerRoleAttribute : Attribute
    {
        public string Role { get; }

        public SwaggerRoleAttribute(string role)
        {
            Role = role;
        }
    }
}