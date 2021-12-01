using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.XPath;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace NetRpc.Http;

public static class XmlHelper
{
    public static void ResetXmlForInnerType(SchemaGeneratorOptions swaggerOptions, DocXmlOptions docXmlOptions, List<ContractInfo> contracts)
    {
        foreach (var path in docXmlOptions.Paths)
        {
            swaggerOptions.SchemaFilters.Add(new XmlCommentsSchemaFilter(ResetXmlForInnerType_InnerTypeMap(path)));
            swaggerOptions.SchemaFilters.Add(new XmlCommentsSchemaFilter(ResetXmlForInnerType_MethodParams(contracts, path)));
        }
    }

    private static XPathDocument ResetXmlForInnerType_InnerTypeMap(string docPath)
    {
        var pathRoot = GetDoc(docPath);
        (XPathNavigator newPathMembers, XmlDocument newDoc) = CreateNewDoc();

        foreach (var i in MergeArgTypeFactory.InnerTypeMap)
        {
            var oldNode = pathRoot.SelectSingleNode($"/doc/members/member[@name='{i.OldStr}']/summary");
            if (oldNode != null)
                newPathMembers.AppendChild(GetXml(i.NewStr!, oldNode.Value));
        }

        return new XPathDocument(new XmlNodeReader(newDoc));
    }

    private static XPathDocument ResetXmlForInnerType_MethodParams(List<ContractInfo> contractInfos, string docPath)
    {
        var rawRoot = GetDoc(docPath);
        (XPathNavigator newPathMembers, XmlDocument newDoc) = CreateNewDoc();

        foreach (var contractInfo in contractInfos)
        foreach (var method in contractInfo.Methods)
        foreach (var rout in method.Route.SwaggerRouts)
            Populate_MethodParams(rawRoot, newPathMembers, rout.MergeArgType);

        return new XPathDocument(new XmlNodeReader(newDoc));
    }

    private static void Populate_MethodParams(XPathNavigator rawRoot, XPathNavigator newPathMembers, MergeArgType type)
    {
        /*
        <member name="M:DataContract.IService2Async.Call3Async(DataContract.CallObj,System.String)">
            <summary>
            Call3Async des
            </summary>
            <param name="obj">obj des</param>
            <param name="s1">s1 des</param>
            <returns></returns>
        </member>

        ->

        <member name="T:DataContract.CallObjXXX.obj">   //ref obj will ignore the summary
            <summary>
            obj des
            </summary>
        </member>
        <member name="T:DataContract.CallObjXXX.s1">
            <summary>
            s1 des
            </summary>
        </member>
        */

        var methodStr = XmlCommentsNodeNameHelper.GetMemberNameForMethod(type.MethodInfo);

        foreach (var p in type.MethodInfo.GetParameters())
        {
            var pPath = rawRoot.SelectSingleNode($"/doc/members/member[@name='{methodStr}']/param[@name='{p.Name}']");
            var prop = type.TypeWithoutPathQueryStream!.GetProperties().FirstOrDefault(i => i.Name == p.Name);
            if (pPath != null && prop != null)
            {
                var str = XmlCommentsNodeNameHelper.GetMemberNameForFieldOrProperty(prop);
                newPathMembers.AppendChild(GetXml(str, pPath.Value));
            }
        }
    }

    private static XPathNavigator GetDoc(string docPath)
    {
        var doc = new XmlDocument();
        doc.Load(docPath);
        var nav = doc.CreateNavigator();
        return nav!;
    }

    private static (XPathNavigator pathMembers, XmlDocument doc) CreateNewDoc()
    {
        var doc = new XmlDocument();
        doc.LoadXml("<doc><members></members></doc>");
        var nav = doc.CreateNavigator();
        var pathMembers = nav!.SelectSingleNode("/doc/members")!;
        return (pathMembers, doc);
    }

    private static string GetXml(string name, string summary)
    {
        return $"<member name=\"{name}\"><summary>{summary}</summary></member>";
    }
}