using System.Text.Json.Serialization;

namespace NetRpc.Http.Client
{
    public sealed class FaultExceptionJsonObj
    {
        [JsonPropertyName("error_code")]
        public string? ErrorCode { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        public FaultExceptionJsonObj(string? errorCode, string? message)
        {
            ErrorCode = errorCode;
            Message = message;
        }

        public FaultExceptionJsonObj()
        {
        }
    }
}