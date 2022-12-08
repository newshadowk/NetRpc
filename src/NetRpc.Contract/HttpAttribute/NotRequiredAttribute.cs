namespace NetRpc.Contract;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = true)]
public class QueryRequiredAttribute : Attribute
{
    public QueryRequiredAttribute()
    {
    }
}