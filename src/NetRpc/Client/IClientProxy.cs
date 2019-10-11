using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NetRpc
{
    public interface IClientProxy<out TService> : IDisposable
    {
        event EventHandler Connected;
        event EventHandler DisConnected;
        event EventHandler<EventArgsT<Exception>> ExceptionInvoked;
        event Func<IClientProxy<TService>, Task> Heartbeat;
        Dictionary<string, object> AdditionHeader { get; set; }
        TService Proxy { get; }
        bool IsConnected { get; }
        void StartHeartbeat(bool isImmediate = false);
        Task HeartbeatAsync();
    }
}