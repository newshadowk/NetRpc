namespace NetRpc.Contract;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class ResponseTextAttribute : Attribute
{
    public ResponseTextAttribute(int statusCode)
    {
        StatusCode = statusCode;
    }

    public int StatusCode { get; }
}