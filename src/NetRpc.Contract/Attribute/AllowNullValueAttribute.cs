namespace NetRpc.Contract;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class CanNullAttribute : Attribute
{
    public CanNullAttribute()
    {
    }
}