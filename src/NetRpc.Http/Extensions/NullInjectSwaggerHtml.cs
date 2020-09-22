namespace NetRpc.Http
{
    public class NullInjectSwaggerHtml : IInjectSwaggerHtml
    {
        public string InjectHtml(string html)
        {
            return html;
        }
    }
}