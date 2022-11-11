using System.Text.Json;
using System.Text.Json.Serialization;
using NetRpc.Http.Client;

namespace NetRpc.Http;

internal class Result
{
    public int StatusCode { get; set; }

    public object? Ret { get; set; }

    public bool IsPainText { get; set; }

    private Result()
    {
    }

    public static Result FromPainText(string? ret, int statusCode)
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
            StatusCode = statusCode
        };
    }

    public Result(object? ret)
    {
        StatusCode = 200;
        Ret = ret;
    }

    public string? ToJson()
    {
        if (Ret == null)
            return null;

        if (IsPainText)
            return Ret.ToString();

        if (StatusCode == 200)
            return Ret.ToDtoJson();

        return JsonSerializer.Serialize(Ret, new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
    }
}