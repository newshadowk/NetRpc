using Grpc.Net.Client;

namespace NetRpc.Grpc;

public class GrpcClientOptions
{
    public GrpcChannelOptions ChannelOptions { get; set; } = new();

    public string Url { get; set; } = null!;

    public string? HeaderHost { get; set; }

    public override string ToString()
    {
        return $"HeaderHost:{HeaderHost}, {Url}";
    }
}