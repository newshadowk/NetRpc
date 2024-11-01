using System.Reflection;

namespace NetRpc;

public class InnerTypeMapItem
{
    public Type? OldType { get; }

    public Type? NewType { get; }

    public PropertyInfo? OldPropertyInfo { get; }

    public PropertyInfo? NewPropertyInfo { get; }

    public string? OldStr { get; }

    public string? NewStr { get; }

    public InnerTypeMapItem(Type oldType, Type newType)
    {
        OldType = oldType;
        NewType = newType;

        OldStr = XmlCommentsNodeNameHelper.GetMemberNameForType(oldType);
        NewStr = XmlCommentsNodeNameHelper.GetMemberNameForType(newType);
    }

    public InnerTypeMapItem(PropertyInfo oldPropertyInfo, PropertyInfo newPropertyInfo)
    {
        OldPropertyInfo = oldPropertyInfo;
        NewPropertyInfo = newPropertyInfo;

        OldStr = XmlCommentsNodeNameHelper.GetMemberNameForFieldOrProperty(OldPropertyInfo);
        NewStr = XmlCommentsNodeNameHelper.GetMemberNameForFieldOrProperty(NewPropertyInfo);
    }
}