using Grpc.Net.Client;

namespace NetRpc.Grpc
{
    public class GrpcClientOptions
    {
        public GrpcChannelOptions ChannelOptions { get; set; } = new();

        public string Url { get; set; } = null!;

        public override string ToString()
        {
            return Url;
        }
    }
}