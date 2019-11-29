﻿using Microsoft.Extensions.Logging.Abstractions;

namespace NetRpc.Http.Client
{
    public static class NetRpcManager
    {
        public static ClientProxy<TService> CreateClientProxy<TService>(HttpClientOptions options, int timeoutInterval = 1200000, int hearbeatInterval = 10000)
        {
            return new ClientProxy<TService>(new HttpOnceCallFactory(options, NullLogger.Instance),
                new SimpleOptionsMonitor<NetRpcClientOption>(new NetRpcClientOption
                    {
                        TimeoutInterval = timeoutInterval,
                        HearbeatInterval = hearbeatInterval
                    }
                ), null, NullLoggerFactory.Instance);
        }
    }
}