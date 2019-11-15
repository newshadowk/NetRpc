using System;

namespace NetRpc
{
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, Inherited = false)]
    public class GrpcIgnoreAttribute : Attribute
    {
        public GrpcIgnoreAttribute()
        {
        }
    }

    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, Inherited = false)]
    public class RabbitMQIgnoreAttribute : Attribute
    {
        public RabbitMQIgnoreAttribute()
        {
        }
    }

    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, Inherited = false)]
    public class HttpIgnoreAttribute : Attribute
    {
        public HttpIgnoreAttribute()
        {
        }
    }

    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, Inherited = false)]
    public class TracerIgnoreAttribute : Attribute
    {
        public TracerIgnoreAttribute()
        {
        }
    }

    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, Inherited = false)]
    public sealed class TracerArgsIgnoreAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, Inherited = false)]
    public sealed class TracerReturnIgnoreAttribute : Attribute
    {
    }
}