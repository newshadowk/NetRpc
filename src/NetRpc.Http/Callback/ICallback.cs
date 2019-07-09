using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace NetRpc.Http
{
    public interface ICallback
    {
        Task Callback(string callId, string data);
    }

    public sealed class CallbackHub : Hub<ICallback>
    {
        public static event EventHandler Canceled;

        public Task<string> GetConnectionId()
        {
            return Task.FromResult(Context.ConnectionId);
        }

        public Task Cancel()
        {
            OnCanceled();
            return Task.CompletedTask;
        }

        private void OnCanceled()
        {
            Canceled?.Invoke(this, EventArgs.Empty);
        }
    }
}