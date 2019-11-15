namespace NetRpc.OpenTracing
{
    public class OpenTracingOptions
    {
        public int LogActionInfoMaxLength { get; set; }

        public bool IsLogDetails { get; set; } = true;

        public bool ForceLogSendStream { get; set; }
    }
}