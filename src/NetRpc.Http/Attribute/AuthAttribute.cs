using System;

namespace NetRpc.Http
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    public class AuthAttribute : Attribute
    {
        public string Key { get; }

        public AuthAttribute(string key)
        {
            Key = key;
        }
    }
}