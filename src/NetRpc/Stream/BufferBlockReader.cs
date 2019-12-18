using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Primitives;

namespace NetRpc
{
    public class BufferBlockReader
    {
        private readonly BufferBlock<(byte[], BufferType)> _block;

        private CacheBuffer lastBuffer;

        private bool _isEnd;

        public BufferBlockReader(BufferBlock<(byte[], BufferType)> block)
        {
            _block = block;
        }

        public int Read(byte[] buffer, int count)
        {
            if (_isEnd)
                return 0;

            var readCount = 0;
            while (readCount < count)
            {
                if (lastBuffer == null || lastBuffer.IsEnd)
                {
                    var value = _block.Receive(TimeSpan.FromSeconds(20));
                    if (value.Item2 == BufferType.End ||
                        value.Item2 == BufferType.Fault)
                    {
                        _isEnd = true;
                        return readCount;
                    }

                    if (value.Item2 == BufferType.Cancel)
                    {
                        _isEnd = true;
                        throw new TaskCanceledException();
                    }

                    lastBuffer = new CacheBuffer(value.Item1);
                }

                var readBufferCount = lastBuffer.Read(buffer, readCount, count - readCount);
                readCount += readBufferCount;
            }

            return readCount;
        }

        public async Task<int> ReadAsync(byte[] buffer, int count, CancellationToken token)
        {
            if (_isEnd)
                return 0;

            var readCount = 0;
            while (readCount < count)
            {
                if (lastBuffer == null || lastBuffer.IsEnd)
                {
                    var value = await _block.ReceiveAsync(TimeSpan.FromSeconds(20), token);
                    if (value.Item2 == BufferType.End ||
                        value.Item2 == BufferType.Fault)
                    {
                        _isEnd = true;
                        return readCount;
                    }

                    if (value.Item2 == BufferType.Cancel)
                    {
                        _isEnd = true;
                        throw new TaskCanceledException();
                    }

                    lastBuffer = new CacheBuffer(value.Item1);
                }

                var readBufferCount = lastBuffer.Read(buffer, readCount, count - readCount);
                readCount += readBufferCount;
            }

            return readCount;
        }
    }
}