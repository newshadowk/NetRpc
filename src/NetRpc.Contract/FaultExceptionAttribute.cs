using System;
using System.IO;

namespace NetRpc
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Interface, AllowMultiple = true, Inherited = false)]
    public sealed class FaultExceptionAttribute : Attribute
    {
        public FaultExceptionAttribute(Type detailType, int statusCode, int errorCode = 0, string summary = null)
        {
            DetailType = detailType;
            StatusCode = statusCode;
            Summary = summary;
            ErrorCode = errorCode;
        }

        public int ErrorCode { get; }

        public Type DetailType { get; }

        public string Summary { get; }

        public int StatusCode { get; }
    }
}