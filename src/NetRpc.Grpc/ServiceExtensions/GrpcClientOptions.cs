using Grpc.Net.Client;

namespace NetRpc.Grpc
{
    /// <summary>
    /// Support OptionName.
    /// </summary>
    public class GrpcClientOptions
    {
        public GrpcChannelOptions? ChannelOptions { get; set; } = new();

        public string? Url { get; set; }

        public override string ToString()
        {
            return Url ?? "";
        }
    }
}