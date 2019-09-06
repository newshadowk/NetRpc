namespace NetRpc.Http.Client
{
    public static class ClientConstValue
    {
        public const string ConnectionIdName = "_connectionId";
        public const string CallIdName = "_callId";
        public const string CustomResultHeaderKey = "result";
        public const int CancelStatusCode = 600;
        public const int DefaultExceptionStatusCode = 400;
    }
}