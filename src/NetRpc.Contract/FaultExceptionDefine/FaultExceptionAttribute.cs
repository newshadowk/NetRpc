using System;
using System.Collections.Generic;

namespace NetRpc.Contract
{
    public interface IFaultExceptionGroup
    {
        List<FaultExceptionDefineAttribute> FaultExceptionDefineAttributes { get; }
    }

    /// <summary>
    /// Pass define FaultException to methods.
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = true)]
    public sealed class InheritedFaultExceptionDefineAttribute : Attribute
    {
    }

    /// <summary>
    /// Show FaultException description in swagger.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Interface, AllowMultiple = true, Inherited = false)]
    public sealed class HideFaultExceptionDescriptionAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Interface, AllowMultiple = true, Inherited = false)]
    public sealed class FaultExceptionAttribute : Attribute
    {
        public FaultExceptionAttribute(Type detailType, int statusCode = 400, string? errorCode = null, string? description = null)
        {
            DetailType = detailType;
            StatusCode = statusCode;
            Description = description;
            ErrorCode = errorCode;
        }

        public string? ErrorCode { get; }

        public Type DetailType { get; }

        public string? Description { get; }

        public int StatusCode { get; }
    }


    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = true)]
    public sealed class FaultExceptionDefineAttribute : Attribute
    {
        public FaultExceptionDefineAttribute(Type detailType, int statusCode, string? errorCode = null, string? description = null)
        {
            DetailType = detailType;
            StatusCode = statusCode;
            Description = description;
            ErrorCode = errorCode;
        }

        public string? ErrorCode { get; }

        public Type DetailType { get; }

        public string? Description { get; }

        public int StatusCode { get; }
    }
}