using System;

namespace NetRpc
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public sealed class FaultExceptionAttribute : Attribute
    {
        public FaultExceptionAttribute(Type detailType, int statusCode)
        {
            DetailType = detailType;
            StatusCode = statusCode;
        }

        public FaultExceptionAttribute(int statusCode)
        {
            StatusCode = statusCode;
        }

        public Type DetailType { get; }

        public int StatusCode { get; }
    }
}