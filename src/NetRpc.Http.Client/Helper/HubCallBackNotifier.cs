using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

namespace NetRpc.Http.Client;

internal sealed class HubCallBackNotifier
{
    private readonly HubConnection _connection;

    public HubCallBackNotifier(HubConnection connection)
    {
        _connection = connection;
    }

    public event EventHandler<CallbackEventArgs>? Callback;

    public void OnCallback(CallbackEventArgs e)
    {
        Callback?.Invoke(this, e);
    }

    public Task CancelAsync(string callId)
    {
        return _connection.InvokeAsync("cancel", callId);
    }
}