using NetRpc.Contract;

namespace NetRpc
{
    public sealed class ConnectionInfo
    {
        public ChannelType ChannelType { get; init; }
        public string? HeadHost { get; init; }
        public string Host { get; init; } = null!;
        public int Port { get; init; }
        public string Description { get; init; } = null!;

        public override string ToString()
        {
            return $"{ChannelType}, {HeadHost}, {Host}:{Port}, {Description}";
        }
    }
}