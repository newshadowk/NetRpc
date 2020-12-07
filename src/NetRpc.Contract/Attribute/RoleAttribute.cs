using System;

namespace NetRpc.Contract
{
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    public class RoleAttribute : Attribute
    {
        public string Role { get; }

        public RoleAttribute(string role)
        {
            Role = role;
        }
    }
}