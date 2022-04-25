using System;
using System.Threading;
using System.Threading.Tasks;

namespace Proxy.RabbitMQ;

public class MsgThreshold
{
    private int _count;

    public void Add()
    {
        Interlocked.Increment(ref _count);
    }

    public async Task Wait(Func<uint> messageCount)
    {
        if (_count <= Const.SubQueueMsgMaxCount)
            return;

        while (messageCount() >= Const.SubQueueMsgMaxCount - 1) 
            await Task.Delay(0);
    }
}