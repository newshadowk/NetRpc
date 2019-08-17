using System;
using System.Diagnostics;

namespace NetRpc.Http
{
    //internal class Result<T> : Result
    //{
    //    public new T Ret 
    //    {
    //        get => (T)base.Ret;
    //        set => base.Ret = value;
    //    }
    //}

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

        //public static Result FromEx(Exception ex, int statusCode)
        //{
        //    if (ex.GetType().IsEqualsOrSubclassOf(typeof(OperationCanceledException)))
        //        return new Result<Exception> {Ret = ex, Type = ResultType.Canceled};
        //    return new Result<Exception> { Ret = ex, Type = ResultType.Fault};
        //}

        //public static Result FromOk<T>(T ret)
        //{
        //    return new Result<T> { Ret = ret, Type = ResultType.Ok};
        //}

        //public Fault ToFault(bool isClearStackTrace)
        //{
        //    var ex = (Exception)Ret;
        //    if (isClearStackTrace)
        //        ex.SetStackTrace(new StackTrace());
                 
        //    Fault fault = new Fault();
        //    fault.Name = ex.GetType().Name;  //there will be issue, when exception name is reduplicate in different namespace.
        //    fault.Details = ex;
        //    return fault;
        //}
    }

    //internal enum ResultType
    //{
    //    Ok,
    //    Canceled,
    //    Fault
    //}
}