namespace NetRpc.Http.Client;

/// <summary>
/// Support OptionName.
/// </summary>
public class HttpClientOptions
{
    public string? SignalRHubUrl { get; set; }
    public string ApiUrl { get; set; } = null!;
}