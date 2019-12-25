using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace NetRpc.Http
{
    public interface ICallback
    {
        Task Callback(string callId, string data);

        Task UploadProgress(string callId, string data);
    }

    public sealed class CallbackHub : Hub<ICallback>
    {
        public static event EventHandler<string> Canceled;

        public Task<string> GetConnectionId()
        {
            return Task.FromResult(Context.ConnectionId);
        }

        public Task Cancel(string callId)
        {
            OnCanceled(callId);
            return Task.CompletedTask;
        }

        private static void OnCanceled(string e)
        {
            Canceled?.Invoke(null, e);
        }
    }
}