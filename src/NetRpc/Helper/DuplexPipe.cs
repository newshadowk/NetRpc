using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading.Tasks;

namespace NetRpc
{
    internal sealed class DuplexPipe : IDuplexPipe, IDisposable
#if NETSTANDARD2_1 || NETCOREAPP3_1
        , IAsyncDisposable
#endif
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

#if NETSTANDARD2_1 || NETCOREAPP3_1
        public async ValueTask DisposeAsync()
        {
             if (OutputStream != null)
                 await OutputStream.DisposeAsync();
             if (InputStream != null)
                 await InputStream.DisposeAsync();
        }
#endif

        public void Dispose()
        {
            OutputStream?.Dispose();
            InputStream?.Dispose();
        }
    }
}