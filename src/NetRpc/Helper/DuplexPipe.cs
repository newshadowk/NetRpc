using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading.Tasks;

namespace NetRpc
{
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
#if NETSTANDARD2_1 || NETCOREAPP3_1
            if (OutputStream != null)
                await OutputStream.DisposeAsync();
            if (InputStream != null)
                await InputStream.DisposeAsync();
#else
            OutputStream?.Dispose();
            InputStream?.Dispose();
#endif
        }
    }
}