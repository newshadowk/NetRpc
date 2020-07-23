namespace NetRpc.Http.Client
{
    /// <summary>
    /// Support OptionName.
    /// </summary>
    public class HttpClientOptions
    {
        public string SignalRHubUrl { get; set; } = null!;
        public string ApiUrl { get; set; } = null!;
    }
}