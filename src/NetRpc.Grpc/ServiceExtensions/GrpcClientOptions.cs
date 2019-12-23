#if NETCOREAPP3_1
using Channel = Grpc.Net.Client.GrpcChannel;
#else
using Channel = Grpc.Core.Channel;
#endif

namespace NetRpc.Grpc
{
    public class GrpcClientOptions
    {
        public Channel Channel { get; set; }
    }
}