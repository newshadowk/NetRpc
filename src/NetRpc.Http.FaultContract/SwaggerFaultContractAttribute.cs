using System;

namespace NetRpc.Http.FaultContract
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public sealed class SwaggerFaultContractAttribute : Attribute
    {
        public SwaggerFaultContractAttribute(Type detailType)
        {
            DetailType = detailType;
        }
       
        public Type DetailType { get; }
    }
}