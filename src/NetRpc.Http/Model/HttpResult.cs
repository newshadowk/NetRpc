using System;

namespace NetRpc.Http
{
    internal class HttpResult
    {
        public string Status { get; set; }

        public string Ret { get; set; } = "";

        public string Msg { get; set; } = "";

        public static HttpResult FromEx(Exception ex)
        {
            return new HttpResult {Msg = ex.Message, Status = ex.GetType().Name, Ret = ex.ToJson()};
        }

        public static HttpResult FromOk(object ret)
        {
            return new HttpResult {Ret = ret.ToJson(), Status = "ok"};
        }
    }
}