using System;
using System.Runtime.Serialization;

namespace NetRpc
{
    [Serializable]
    public class MethodNotFoundException : Exception
    {
        public MethodNotFoundException()
        {
        }

        protected MethodNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public MethodNotFoundException(string message) : base(message)
        {
        }
    }

    [Serializable]
    public sealed class FaultException<T> : FaultException where T : Exception
    {
        public new T Detail
        {
            get => (T)base.Detail;
            set => base.Detail = value;
        }

        public new string Action
        {
            get => base.Action;
            set => base.Action = value;
        }

        public FaultException()
        {
        }

        public FaultException(T detail) : base(detail)
        {
        }

        private FaultException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    [Serializable]
    public class FaultException : Exception
    {
        public Exception Detail { get; set; }

        public string Action { get; set; }

        public FaultException()
        {
        }

        public FaultException(Exception detail)
        {
            Detail = detail;
        }

        protected FaultException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            this.SetObjectData(info);
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            this.GetObjectData(info);
        }

        public override string ToString()
        {
            return $"Action:{Action}, Detail:{Helper.ExceptionToString(Detail)}";
        }
    }
}