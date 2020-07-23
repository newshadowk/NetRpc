namespace NetRpc.Http.Client
{
    public sealed class FaultExceptionJsonObj
    {
        public int ErrorCode { get; set; }

        public string? Message { get; set; }

        public FaultExceptionJsonObj(int errorCode, string message)
        {
            ErrorCode = errorCode;
            Message = message;
        }

        public FaultExceptionJsonObj()
        {
        }
    }
}