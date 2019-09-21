using System;

namespace NetRpc
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Interface, AllowMultiple = true, Inherited = false)]
    public sealed class FaultExceptionAttribute : Attribute
    {
        public FaultExceptionAttribute(Type detailType, int statusCode = 400, int errorCode = 0, string summary = null)
        {
            DetailType = detailType;
            StatusCode = statusCode;
            Summary = summary;
            ErrorCode = errorCode;
        }

        public int ErrorCode { get; set; }

        public Type DetailType { get; set; }

        public string Summary { get; set; }

        public int StatusCode { get; set; }
    }


    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = true)]
    public sealed class FaultExceptionDefineAttribute : Attribute
    {
        public FaultExceptionDefineAttribute(Type detailType, int statusCode, int errorCode = 0, string summary = null)
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