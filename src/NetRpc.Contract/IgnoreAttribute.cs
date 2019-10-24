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
    public class JaegerIgnoreAttribute : Attribute
    {
        public JaegerIgnoreAttribute()
        {
        }
    }
}