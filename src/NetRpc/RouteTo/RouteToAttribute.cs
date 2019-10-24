using System;

namespace NetRpc
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Interface)]
    public class RouteToAttribute : Attribute
    {
        private readonly Type _contactType;
        private readonly string _methodName;

        public RouteToAttribute(Type contactType, string methodName)
        {
            _contactType = contactType;
            _methodName = methodName;
        }
    }
}