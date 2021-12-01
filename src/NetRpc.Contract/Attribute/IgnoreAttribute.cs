using System;

namespace NetRpc.Contract;

[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, Inherited = false)]
public class GrpcIgnoreAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, Inherited = false)]
public class RabbitMQIgnoreAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, Inherited = false)]
public class HttpIgnoreAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, Inherited = false)]
public class TracerIgnoreAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, Inherited = false)]
public sealed class TracerArgsIgnoreAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, Inherited = false)]
public sealed class TracerReturnIgnoreAttribute : Attribute
{
}