using NetRpc.Http.Client;

namespace NetRpc.Http
{
    internal class Result
    {
        public int StatusCode { get; set; }

        public object Ret { get; set; }

        public int ErrorCode { get; set; }

        public bool IsPainText { get; set; }

        public Result(object ret, int statusCode, int errorCode = 0)
        {
            StatusCode = statusCode;
            ErrorCode = errorCode;
            Ret = ret;
        }

        public Result()
        {
        }

        public static Result FromPainText(string ret, int statusCode)
        {
            return new Result
            {
                Ret = ret,
                IsPainText = true,
                StatusCode = statusCode
            };
        }

        public static Result FromFaultException(FaultExceptionJsonObj obj, int statusCode)
        {
            return new Result
            {
                Ret = obj,
                StatusCode = statusCode,
                ErrorCode = obj.ErrorCode
            };
        }

        public Result(object ret)
        {
            StatusCode = 200;
            Ret = ret;
        }
    }
}