using System;

namespace NetRpc
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Interface, AllowMultiple = true, Inherited = false)]
    public sealed class FaultExceptionAttribute : Attribute
    {
        public FaultExceptionAttribute(Type detailType, int statusCode, string summary = null)
        {
            DetailType = detailType;
            StatusCode = statusCode;
            Summary = summary;
        }

        public FaultExceptionAttribute(int statusCode)
        {
            StatusCode = statusCode;
        }

        public Type DetailType { get; }

        public string Summary { get; }

        public int StatusCode { get; }
    }
}