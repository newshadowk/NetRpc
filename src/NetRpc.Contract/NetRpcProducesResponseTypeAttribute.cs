using System;

namespace NetRpc
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public sealed class NetRpcProducesResponseTypeAttribute : Attribute
    {
        public NetRpcProducesResponseTypeAttribute(Type detailType, int statusCode)
        {
            DetailType = detailType;
            StatusCode = statusCode;
        }

        public NetRpcProducesResponseTypeAttribute(int statusCode)
        {
            StatusCode = statusCode;
        }

        public Type DetailType { get; }

        public int StatusCode { get; }
    }
}