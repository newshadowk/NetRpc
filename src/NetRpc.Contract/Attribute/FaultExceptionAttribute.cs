using System;

namespace NetRpc.Contract
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Interface, AllowMultiple = true, Inherited = false)]
    public sealed class FaultExceptionAttribute : Attribute
    {
        public FaultExceptionAttribute(Type detailType, int statusCode = 400, int errorCode = 0, string? description = null, bool clearHttpMessage = false)
        {
            ClearHttpMessage = clearHttpMessage;
            DetailType = detailType;
            StatusCode = statusCode;
            Description = description;
            ErrorCode = errorCode;
        }

        public int ErrorCode { get; set; }

        public bool ClearHttpMessage { get; set; }

        public Type DetailType { get; set; }

        public string? Description { get; set; }

        public int StatusCode { get; set; }
    }


    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = true)]
    public sealed class FaultExceptionDefineAttribute : Attribute
    {
        public FaultExceptionDefineAttribute(Type detailType, int statusCode, int errorCode = 0, string? description = null, bool clearHttpMessage = false)
        {
            ClearHttpMessage = clearHttpMessage;
            DetailType = detailType;
            StatusCode = statusCode;
            Description = description;
            ErrorCode = errorCode;
        }

        public int ErrorCode { get; }

        public bool ClearHttpMessage { get; }

        public Type DetailType { get; }

        public string? Description { get; }

        public int StatusCode { get; }
    }
}