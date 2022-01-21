using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using NetRpc.Contract;

namespace DataContract;

public sealed class FaultExceptionDefineGroupAttribute : Attribute, IFaultExceptionGroup
{
    public List<FaultExceptionDefineAttribute> FaultExceptionDefineAttributes =>
        new()
        {
            new FaultExceptionDefineAttribute(typeof(CustomException), 400, "1", "errorCode1 error description"),
            new FaultExceptionDefineAttribute(typeof(CustomException2), 400, "2", "errorCode2 error description")
        };
}

[HttpTrimAsync]
[HttpRoute("Service")]
[FaultExceptionDefine(typeof(CustomException), 400, "1", "errorCode1 error description")]
[FaultExceptionDefine(typeof(CustomException2), 400, "2", "errorCode2 error description")]
[HttpHeader("h1", "h1 des.")]
[SecurityApiKeyDefine("tokenKey", "t1", "t1 des")]
[SecurityApiKeyDefine("tokenKey2", "t2", "t2 des")]
[SwaggerRole("RAll")]
public interface IServiceAsync
{
    [Example("s1", "s1value")]
    [Example("s2", "s2value")]
    [SwaggerRole("R1")]
    [SwaggerRole("R3")]
    Task<CustomObj> Call2Async(CObj obj, string s1, string s2);

    /// <summary>
    /// summary of Call
    /// </summary>
    /// <response code="201">Returns the newly created item</response>
    [HttpRoute("Service1/Call2")]
    [HttpHeader("h2", "h2 des.")]
    [SecurityApiKey("tokenKey")]
    [SwaggerRole("R1,!RAll")]
    Task<CustomObj> CallAsync(string p1, int p2);

    [SwaggerRole("R2,R3")]
    Task Call3Async(SimObj obj);

    //[HttpRoute("Service1/{p1}/Call4")]
    Task<string> Call4Async(string p1, int p2);

    /// <summary>
    /// summary of Call
    /// </summary>
    [FaultException(typeof(CustomException))]
    [FaultException(typeof(CustomException2), 400, "2")]
    [Tag("A1")]
    [Tag("A2")]
    Task CallByCustomExceptionAsync();

    Task CallByDefaultExceptionAsync();

    [Tag("A1")]
    Task CallByCancelAsync(CancellationToken token);

    /// <response code="701">return the pain text.</response>
    [ResponseText(701)]
    Task CallByResponseTextExceptionAsync();

    Task<ComplexStream> ComplexCallAsync(CustomObj obj, string p1, Stream stream, Func<CustomCallbackObj, Task> cb, CancellationToken token);

    Task<int> UploadAsync(Stream stream, string streamName, string p1, Func<int, Task> cb, CancellationToken token);
}

[HttpTrimAsync]
[HttpRoute("IRout1")]
[Tag("RoutTag1")]
public interface IService2Async
{
    Task<CObj> CallNone(CObj obj, Func<double, Task> cb, Stream stream);

    //Task<string> CallNone2(CObj obj);

    //[HttpGet("CallNone3")]

    /// <response code="200">return the pain text.</response>
    //[HttpGet]
    //Task<string> CallNone3();
    [Tag("CallTag1")]
    [HttpPost]
    [HttpRoute("Call1/{p1}")]
    [HttpGet("/Root/Call/{p1}")]
    [HttpTrimAsync]
    Task<string> Call1Async(string p1, int p2);

    //[HttpGet]
    //[HttpDelete]
    //[HttpHead]
    //[HttpPut]
    //[HttpPatch]
    //[HttpOptions]
    //[HttpGet("Call2/{P1}/{P2}/Get")]
    //[HttpPost("Call2/{P1}/Post")]
    //Task<string> Call2Async(CallObj obj);

    ///// <summary>
    ///// Call3Async des
    ///// </summary>
    ///// <param name="obj">obj des</param>
    ///// <param name="s1">s1 des</param>
    ///// <returns></returns>
    //[HttpPost("Call3/{s1}")]
    //Task<string> Call3Async(CallObj obj, string s1);

    //Task<string> Call3Async(CallObj obj);

    ///// <summary>
    ///// Call3Async des
    ///// </summary>
    ///// <param name="obj">obj des</param>
    ///// <param name="s1">s1 des</param>
    ///// <param name="cb">cb des</param>
    ///// <param name="token">token des</param>
    ///// <returns></returns>
    //Task<string> Call3Async(CallObj obj, string s1, Func<double, Task> cb, CancellationToken token);

    //[HttpGet]
    //Task<string> Call4Async(CallObj obj, Func<double, Task> cb, CancellationToken token);

    //Task<string> Call5Async(string p1, int p2, Func<double, Task> cb, CancellationToken token);
}

[FaultExceptionDefine(typeof(CustomException), 300, "1", "errorCode1 error description")]
//[FaultExceptionDefine(typeof(CustomException2), 400, "2", "errorCode2 error description")]
public interface IService4Async
{
    //[HttpPatch("call/{TaskId}")]
    //Task<string> Call(Obj5 obj5);
    [FaultException(typeof(CustomException))]
    [HttpGet("ValidateInviteCode/{code}?lang={lang}")]
    Task<object> ValidateInviteCodeAsync(string code, string lang);

    //[HttpPost]
    //Task<string> Call(CancellationToken token);
}

public class Obj5
{
    public string TaskId { get;set; }
    //public string TaskId2 { get;set; }
}

[Serializable]
public class Obj4
{
    ///// <summary>
    ///// Obj41 des
    ///// </summary>
    //[JsonPropertyName("obj-41")]
    //public Obj41 Obj41 { get; set; }
    //public DateTime dt { get; set; }
    public int? MinOptCount { get; set; }

    public DateTimeOffset? dt2 { get; set; }

    //[JsonPropertyName("items-re")]
    //public ICollection<E1> Items { get; set; }
}

public enum E1
{
    Ev1,
    Ev2
}

[Serializable]
public class Obj41
{
    [JsonPropertyName("p-2")]
    public string P2 { get; set; }
}

/// <summary>
/// CallObj summary
/// </summary>
[Serializable]
public class CallObj
{
    /// <summary>
    /// test p1
    /// </summary>
    public string P1 { get; set; }

    /// <summary>
    /// test p2
    /// </summary>
    public int P2 { get; set; }
}

[Serializable]
public class InnerObj2
{
    public string P3 { get; set; }

    public int I4 { get; set; }
}

[HttpTrimAsync]
public interface IService3Async
{
    Task CallAsync();

    Task Call2Async();

    Task Call3Async();

    Task Call4Async();
}