using System.Threading.Tasks.Dataflow;

namespace Proxy.RabbitMQ;

public class CheckWriteOnceBlock<T>
{
    public WriteOnceBlock<T> WriteOnceBlock { get; } = new(null);

    public object SyncRoot { get; } = new();

    public bool IsPosted { get; set; }
}