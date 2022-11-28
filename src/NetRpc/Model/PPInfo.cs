using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.Json.Serialization;
using NetRpc.Contract;

namespace NetRpc;

public class PPInfo
{
    public PropertyInfo? PropertyInfo { get; }

    public ParameterInfo? ParameterInfo { get; }

    public bool NotRequired { get; }

    public Type Type { get; }

    public string Name { get; }

    public string DefineName { get; }

    public CustomAttributeBuilder? JsonCustomAttributeBuilder { get; }

    public PPInfo(PropertyInfo propertyInfo)
    {
        var jsonP = propertyInfo.GetCustomAttribute<JsonPropertyNameAttribute>();
        if (jsonP != null)
        {
            DefineName = jsonP.Name;
            JsonCustomAttributeBuilder = GetJsonBuilder(DefineName);
        }
        else
            DefineName = propertyInfo.Name;

        Name = propertyInfo.Name;
        PropertyInfo = propertyInfo;
        Type = propertyInfo.PropertyType;

        var attr = propertyInfo.GetCustomAttribute<NotRequiredAttribute>();
        if (attr != null) 
            NotRequired = true;

        if (propertyInfo is { PropertyType.IsGenericType: true } &&
            propertyInfo.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            NotRequired = true;
        }
    }

    public PPInfo(ParameterInfo parameterInfo)
    {
        var jpn = parameterInfo.GetCustomAttribute<JsonParamNameAttribute>();
        if (jpn != null)
        {
            JsonCustomAttributeBuilder = GetJsonBuilder(jpn.Name);
            DefineName = jpn.Name;
        }

        DefineName ??= parameterInfo.Name!;

        Name = parameterInfo.Name!;
        ParameterInfo = parameterInfo;
        Type = parameterInfo.ParameterType;

        var attr = parameterInfo.GetCustomAttribute<NotRequiredAttribute>();
        if (attr != null) 
            NotRequired = true;

        if (parameterInfo is { ParameterType.IsGenericType: true } &&
            parameterInfo.ParameterType.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            NotRequired = true;
        }
    }

    public PPInfo(string name, Type type)
    {
        Type = type;
        Name = name;
        DefineName = name;
    }

    private static CustomAttributeBuilder GetJsonBuilder(string name)
    {
        Type[] ctorParams = { typeof(string) };

        var classCtorInfo = typeof(JsonPropertyNameAttribute).GetConstructor(ctorParams);
        return new CustomAttributeBuilder(
            classCtorInfo!,
            new object[] { name });
    }
}