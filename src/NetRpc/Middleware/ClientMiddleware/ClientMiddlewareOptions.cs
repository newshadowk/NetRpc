namespace NetRpc;

public class ClientMiddlewareOptions
{
    public List<(Type Type, object[] args)> Items { get; set; } = new();

    public void UseMiddleware<TMiddleware>(params object[] args)
    {
        UseMiddleware(typeof(TMiddleware), args);
    }

    public void UseMiddleware(Type type, params object[] args)
    {
        Items.Add((type, args));
    }
}