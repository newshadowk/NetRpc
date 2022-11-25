namespace NetRpc.Contract;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class AllowNullValueAttribute : Attribute
{
    public AllowNullValueAttribute()
    {
    }
}