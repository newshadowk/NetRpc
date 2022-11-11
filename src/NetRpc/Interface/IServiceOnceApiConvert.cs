namespace NetRpc;

public interface IServiceOnceApiConvert : IAsyncDisposable
{
    Task SendBufferAsync(ReadOnlyMemory<byte> buffer);

    Task SendBufferEndAsync();

    Task SendBufferCancelAsync();

    Task SendBufferFaultAsync();

    Task<bool> StartAsync(CancellationTokenSource cts);

    Task<ServiceOnceCallParam> GetServiceOnceCallParamAsync();

    /// <returns>True need send stream next, otherwise false.</returns>
    Task SendResultAsync(CustomResult result, Stream? stream, string? streamName, ActionExecutingContext context);

    Task SendFaultAsync(Exception body, ActionExecutingContext? context);

    Task SendCallbackAsync(object? callbackObj);
}