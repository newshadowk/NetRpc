namespace NetRpc.Grpc
{
    public sealed class GrpcClientConnectionFactoryOptions
    {
        public GrpcClientConnectionFactory Factory { get; }

        public GrpcClientConnectionFactoryOptions(GrpcClientOptions options)
        {
            Factory = new GrpcClientConnectionFactory(new SimpleOptionsMonitor<GrpcClientOptions>(options));
        }
    }
}