using System;

namespace NetRpc.Http.Client
{
    internal sealed class CallbackEventArgs : EventArgs
    {
        public string CallId { get; }
        public string Data { get; }

        public CallbackEventArgs(string callId, string data)
        {
            CallId = callId;
            Data = data;
        }
    }
}