using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NetRpc
{
    public interface IClientProxy<out TService> : IClientProxy where TService : class
    {
        new TService Proxy { get; }
    }

    public interface IClientProxy : IDisposable
    {
        event EventHandler? Connected;
        event EventHandler? DisConnected;
        event EventHandler<EventArgsT<Exception>>? ExceptionInvoked;
        event AsyncEventHandler? HeartbeatAsync;
        Dictionary<string, object?> AdditionHeader { get; set; }
        object Proxy { get; }
        bool IsConnected { get; }
        void StartHeartbeat(bool isImmediate = false);
        void StopHeartBeat();
        Task InvokeHeartbeatAsync();
    }
}