using System;

namespace NetRpc
{
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, Inherited = false)]
    public class HttpRouteAttribute : Attribute
    {
        public HttpRouteAttribute(string template, bool trimActionAsync)
        {
            Template = template ?? throw new ArgumentNullException(nameof(template));

            Template = Template.Replace('\\', '/');

            if (Template.StartsWith("/"))
                Template = template.Substring(1);

            if (Template.EndsWith("\\"))
                Template = Template.Substring(0, Template.Length - 1);

            TrimActionAsync = trimActionAsync;
        }

        public HttpRouteAttribute(string template) : this(template, false)
        {
        }

        public HttpRouteAttribute(bool trimActionAsync)
        {
            TrimActionAsync = trimActionAsync;
        }

        public string Template { get; }

        public bool TrimActionAsync { get; }
    }
}