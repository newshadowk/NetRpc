namespace NetRpc;

[Serializable]
public sealed class ActionInfo
{
    public string FullName { get; set; } = null!;

    public string[] GenericArguments { get; set; } = Array.Empty<string>();

    public override string ToString()
    {
        return $"{FullName}<{GenericArguments.ListToString(",")}>";
    }
}