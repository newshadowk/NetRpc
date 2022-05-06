
using System;
using System.Runtime.Serialization;

namespace NetRpc.Contract;

[Serializable]
public sealed class FaultException<T> : FaultException where T : Exception
{
    public new T? Detail
    {
        get => (T) base.Detail!;
        set => base.Detail = value;
    }

    public new string? Action
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
    public Exception? Detail { get; set; }

    public string? FaultCode { get; set; }

    public string? Action { get; set; }

    public FaultException()
    {
    }

    public FaultException(Exception detail)
    {
        Detail = detail;
    }

    public FaultException(Exception detail, string faultCode, string action)
    {
        Detail = detail;
        FaultCode = faultCode;
        Action = action;
    }

    public FaultException(string faultCode, string message) : base(message)
    {
        FaultCode = faultCode;
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
        return $"{nameof(FaultCode)}:{FaultCode}, {nameof(Action)}:{Action}, {nameof(Detail)}:{Helper.ExceptionToString(Detail)}";
    }
}