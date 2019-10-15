using System;

namespace NetRpc
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class GrpcIgnoreAttribute : Attribute
    {
        public GrpcIgnoreAttribute()
        {
        }
    }


    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class RabbitMQIgnoreAttribute : Attribute
    {
        public RabbitMQIgnoreAttribute()
        {
        }
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class HttpIgnoreAttribute : Attribute
    {
        public HttpIgnoreAttribute()
        {
        }
    }
}