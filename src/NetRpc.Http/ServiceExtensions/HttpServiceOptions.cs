namespace NetRpc.Http
{
    public sealed class HttpServiceOptions
    {
        /// <summary>
        /// Api root path, like '/api', default value is null.
        /// </summary>
        public string? ApiRootPath { get; set; }

        /// <summary>
        /// Set true will pass to next middleware when not match the method, default value is false.
        /// </summary>
        public bool IgnoreWhenNotMatched { get; set; }
    }
}