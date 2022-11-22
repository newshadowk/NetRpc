namespace NetRpc;

[Serializable]
public sealed class CustomResult
{
    public object? Result { get; set; }

    public long StreamLength { get; set; }

    public bool HasStream { get; set; }

    public bool IsImage { get; set; }

    public CustomResult(object? result, bool hasStream, bool isImage, long streamLength)
    {
        Result = result;
        HasStream = hasStream;
        IsImage = isImage;
        StreamLength = streamLength;
    }
}