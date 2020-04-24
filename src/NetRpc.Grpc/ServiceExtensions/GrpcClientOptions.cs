#if NETCOREAPP3_1
using Grpc.Net.Client;
#endif
namespace NetRpc.Grpc
{
    /// <summary>
    /// Support OptionName.
    /// </summary>
    public class GrpcClientOptions
    {
#if NETCOREAPP3_1
        public GrpcChannelOptions ChannelOptions { get; set; } = new GrpcChannelOptions();

        public string Url { get; set; }

        public override string ToString()
        {
            return Url;
        }
#else
        public string Host { get; set; }
        public int Port { get; set; }
        public string PublicKey { get; set; }
        public string SslTargetName { get; set; }

        public override string ToString()
        {
            return $"{Host}:{Port}, {SslTargetName}";
        }
#endif
    }
}