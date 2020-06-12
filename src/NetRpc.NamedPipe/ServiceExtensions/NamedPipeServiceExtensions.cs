using System;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NetRpc;
using NetRpc.Grpc;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class NamedPipeServiceExtensions
    {
    }

    public class NamePipeServiceOptions
    {
        public int Count { get; set; }

        public string Name { get; set; }
    }

    public class NamePipeClientOptions
    {
        public string Name { get; set; }
    }
}