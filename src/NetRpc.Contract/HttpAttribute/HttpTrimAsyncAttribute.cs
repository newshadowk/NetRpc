using System;

namespace NetRpc.Contract
{
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, Inherited = false)]
    public class HttpTrimAsyncAttribute : Attribute
    {

    }
}