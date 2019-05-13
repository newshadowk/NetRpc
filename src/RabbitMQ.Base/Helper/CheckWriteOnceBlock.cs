using System.Threading.Tasks.Dataflow;

namespace RabbitMQ.Base
{
    public class CheckWriteOnceBlock<T>
    {
        public WriteOnceBlock<T> WriteOnceBlock { get; } = new WriteOnceBlock<T>(null);

        public object SyncRoot { get; } = new object();

        public bool IsPosted { get; set; }
    }
}