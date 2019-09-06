namespace NetRpc.Http
{
    internal class Result
    {
        public int StatusCode { get; set; }

        public object Ret { get; set; }

        public Result(object ret, int statusCode)
        {
            StatusCode = statusCode;
            Ret = ret;
        }

        public Result(object ret)
        {
            StatusCode = 200;
            Ret = ret;
        }
    }
}