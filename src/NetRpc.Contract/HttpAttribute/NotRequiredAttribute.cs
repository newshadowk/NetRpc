namespace NetRpc.Contract;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = true)]
public class NotRequiredAttribute : Attribute
{
    public NotRequiredAttribute()
    {
    }
}