using NetRpc.Http;

namespace NetRpc.MiniProfiler
{
    public class InjectSwaggerHtml : IInjectSwaggerHtml
    {
        public string InjectHtml(string html)
        {
            html =
                $"<script async=\"async\" id=\"mini-profiler\" src=\"/profiler/includes.min.js?v=4.0.138+gcc91adf599\" data-version=\"4.0.138+gcc91adf599\" data-path=\"/profiler/\" data-current-id=\"4ec7c742-49d4-4eaf-8281-3c1e0efa748a\" data-ids=\"\" data-position=\"Left\" data-authorized=\"true\" data-max-traces=\"15\" data-toggle-shortcut=\"Alt+P\" data-trivial-milliseconds=\"2.0\" data-scheme=\"light\" data-ignored-duplicate-execute-types=\"Open,OpenAsync,Close,CloseAsync\"></script>{html}";
            return html;
        }
    }
}