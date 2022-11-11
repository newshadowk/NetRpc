using System.IO.Pipelines;

namespace NetRpc;

internal sealed class DuplexPipe : IDuplexPipe, IAsyncDisposable
{
    public PipeReader Input { get; }

    public Stream InputStream { get; }

    public PipeWriter Output { get; }

    public Stream OutputStream { get; }

    public DuplexPipe(PipeOptions options)
    {
        var pipe = new Pipe(options);
        Input = pipe.Reader;
        Output = pipe.Writer;

        InputStream = Input.AsStream();
        OutputStream = Output.AsStream();
    }

    public async ValueTask DisposeAsync()
    {
        await OutputStream.DisposeAsync();
        await InputStream.DisposeAsync();
    }
}