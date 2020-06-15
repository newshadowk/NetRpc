using System;
using System.Linq;

namespace NetRpc.Http
{
    internal sealed class HttpObj
    {
        public HttpDataObj HttpDataObj { get; set; } = new HttpDataObj();

        public ProxyStream ProxyStream { get; set; }
    }

    internal sealed class HttpDataObj
    {
        public string ConnectionId { get; set; }

        public string CallId { get; set; }

        public long StreamLength { get; set; }

        public object Value { get; set; }

        public Type Type { get; set; }

        public bool TrySetStreamName(string streamName)
        {
            Value.GetType().GetProperties().ToList().ForEach(i =>
            {
                Console.WriteLine($"{i.Name}");
            });
            var f = Value.GetType().GetProperties().FirstOrDefault(i => i.Name.IsStreamName());
            if (f == null)
                return false;

            try
            {
                f.SetValue(Value, streamName);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}