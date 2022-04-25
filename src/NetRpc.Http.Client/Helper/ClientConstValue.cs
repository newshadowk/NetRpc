namespace NetRpc.Http.Client;

public static class ClientConst
{
    public const string ConnIdName = "_conn_id";
    public const string CallIdName = "_call_id";
    public const string StreamLength = "_stream_length";
    public const string CustomResultHeaderKey = "result";
    public const int CancelStatusCode = 600;
    public const int DefaultExceptionStatusCode = 400;
}