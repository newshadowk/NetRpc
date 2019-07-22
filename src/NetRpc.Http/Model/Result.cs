using System;
using Namotion.Reflection;

namespace NetRpc.Http
{
    internal class Result<T> : Result
    {
        public new T Ret 
        {
            get => (T)base.Ret;
            set => base.Ret = value;
        }
    }

    internal class Result
    {
        public bool IsSuccessful { get; set; }

        public object Ret { get; set; }

        public static Result<Exception> FromEx(Exception ex)
        {
            return new Result<Exception> { Ret = ex };
        }

        public static Result<T> FromOk<T>(T ret)
        {
            return new Result<T> { Ret = ret, IsSuccessful = true };
        }

        public Fault ToFault()
        {
            var ex = (Exception)Ret;
            Fault fault = new Fault();
            fault.Name = ex.GetType().Name;  //there will be issue, when exception name is reduplicate in different namespace.
            fault.Details = ex;
            return fault;
        }
    }

    internal class Fault
    {
        public string Name { get; set; }

        public object Details { get; set; }
    }
}