namespace NetRpc.Contract;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Parameter | AttributeTargets.Property)]
public abstract class ValidateValueAttribute : Attribute
{
    public abstract void Validate(object? value);
}

[AttributeUsage(AttributeTargets.Class)]
public class ValidateAttribute : Attribute
{
}