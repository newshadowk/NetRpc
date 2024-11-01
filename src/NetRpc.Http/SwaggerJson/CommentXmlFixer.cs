using Microsoft.Extensions.Options;
using System.Xml;
using System.Xml.XPath;

namespace NetRpc.Http;

public class CommentXmlFixer
{
    private readonly List<XPathNavigator> _xns = new();

    public CommentXmlFixer(IOptions<DocXmlOptions> docXmlOptions)
    {
        foreach (var path in docXmlOptions.Value.Paths) 
            _xns.Add(GetDoc(path));
    }

    public string? GetXmlDes(PPInfo ppInfo)
    {
        foreach (var i in MergeArgTypeFactory.InnerTypeMap)
        {
            if (i.OldPropertyInfo == null)
                continue;

            if (i.OldPropertyInfo!.PropertyType == ppInfo.Type)
                return FindSummary(i.OldStr);
        }

        return null;
    }

    private string? FindSummary(string? name)
    {
        if (name == null)
            return null;

        foreach (var navigator in _xns)
        {
            var node = navigator.SelectSingleNode($"/doc/members/member[@name='{name}']/summary");
            if (node != null)
                return node.Value.Trim();
        }

        return null;
    }

    private static XPathNavigator GetDoc(string docPath)
    {
        var doc = new XmlDocument();
        doc.Load(docPath);
        var nav = doc.CreateNavigator();
        return nav!;
    }
}