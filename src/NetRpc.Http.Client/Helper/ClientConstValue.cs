namespace NetRpc.Http.Client
{
    public static class ClientConstValue
    {
        public const string ConnIdName = "_connId";
        public const string CallIdName = "_callId";
        public const string StreamLength = "_streamLength";
        public const string CustomResultHeaderKey = "result";
        public const int CancelStatusCode = 600;
        public const int DefaultExceptionStatusCode = 400;
    }
}