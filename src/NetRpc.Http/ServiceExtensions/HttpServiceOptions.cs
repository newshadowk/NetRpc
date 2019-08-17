
namespace NetRpc.Http
{
    public sealed class HttpServiceOptions
    {
        /// <summary>
        /// Default value is null.
        /// </summary>
        public string ApiRootPath { get; set; }

        /// <summary>
        /// Default value is false.
        /// </summary>
        public bool IsClearStackTrace { get; set; }

        /// <summary>
        /// Default value is false.
        /// </summary>
        public bool IgnoreWhenNotMatched { get; set; }
    }
}