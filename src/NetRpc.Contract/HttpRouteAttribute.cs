using System;

namespace NetRpc
{
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, Inherited = false)]
    public class HttpRouteAttribute : Attribute
    {
        public HttpRouteAttribute(string template, bool trimActionAsync = false)
        {
            Template = template ?? throw new ArgumentNullException(nameof(template));

            Template = Template.Replace('\\', '/');

            if (Template.StartsWith("/"))
                Template = template.Substring(1);

            if (Template.EndsWith("\\"))
                Template = Template.Substring(0, Template.Length - 1);

            TrimActionAsync = trimActionAsync;
        }

        public string Template { get; }

        public bool TrimActionAsync { get; }
    }
}