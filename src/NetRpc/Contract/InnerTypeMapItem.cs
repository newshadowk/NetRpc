using System.Reflection;

namespace NetRpc;

public class InnerTypeMapItem
{
    public Type? OldType { get; }

    public Type? NewType { get; }

    public MemberInfo? OldMemberInfo { get; }

    public MemberInfo? NewMemberInfo { get; }

    public string? OldStr { get; }

    public string? NewStr { get; }

    public InnerTypeMapItem(Type oldType, Type newType)
    {
        OldType = oldType;
        NewType = newType;

        OldStr = XmlCommentsNodeNameHelper.GetMemberNameForType(oldType);
        NewStr = XmlCommentsNodeNameHelper.GetMemberNameForType(newType);
    }

    public InnerTypeMapItem(MemberInfo oldMemberInfo, MemberInfo newMemberInfo)
    {
        OldMemberInfo = oldMemberInfo;
        NewMemberInfo = newMemberInfo;

        OldStr = XmlCommentsNodeNameHelper.GetMemberNameForFieldOrProperty(OldMemberInfo);
        NewStr = XmlCommentsNodeNameHelper.GetMemberNameForFieldOrProperty(NewMemberInfo);
    }
}