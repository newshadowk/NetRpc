using System.Collections;
using System.Reflection;

namespace NetRpc.Http;

public static class MapExtension
{
    public static List<T> MapTo<T>(this IList fromList) where T : new()
    {
        List<T> l = new();
        foreach (var i in fromList) 
            l.Add(i.MapTo<T>());
        return l;
    }

    public static T MapTo<T>(this object fromObj, bool ignoreNullValue = false) where T : new()
    {
        T toObj = new T();
        toObj.MapFrom(fromObj, ignoreNullValue);
        return toObj;
    }

    public static void MapFrom(this object toObj, object? fromObj, bool ignoreNullValue = false)
    {
        if (fromObj is null)
            return;

        List<PropertyInfo> toPs = toObj.GetType().GetProperties().OrderBy(o => o.Name).ToList();
        List<PropertyInfo> fromPs = fromObj.GetType().GetProperties().OrderBy(o => o.Name).ToList();

        foreach (PropertyInfo fromP in fromPs)
        {
            var toP = toPs.Find(i => string.Equals(i.Name, fromP.Name, StringComparison.CurrentCultureIgnoreCase));
            if (toP == null)
                continue;

            var fromValue = fromP.GetValue(fromObj, null);
            if (ignoreNullValue && fromValue == null)
                continue;

            var enumType = GetEnumType(toP.PropertyType);
            if (enumType != null)
                SetEnumValue(toP, enumType, toObj, fromValue);
            else if (toP.PropertyType.IsSystemType() || fromP.PropertyType == toP.PropertyType)
                toP.SetValue(toObj, fromValue, null);
            else
            {
                var toValue = toP.GetValue(toObj);
                if (toValue == null)
                    toValue = Activator.CreateInstance(toP.PropertyType)!;

                MapFrom(toValue, fromValue, ignoreNullValue);
                toP.SetValue(toObj, toValue, null);
            }
        }
    }

    public static void SetEnumValue(PropertyInfo pi, Type enumType, object toObj, object? fromValue)
    {
        var enumValue = GetEnumValue(enumType, fromValue);
        pi.SetValue(toObj, enumValue, null);
    }

    public static object? GetEnumValue(Type enumType, object? fromValue)
    {
        if (fromValue == null)
            return fromValue;
     
        if (fromValue is string valueStr)
        {
            if (Enum.TryParse(enumType, valueStr, true, out var enumV)) 
                fromValue = enumV;
        }
        else
            fromValue = Enum.ToObject(enumType, fromValue);

        return fromValue;
    }

    public static Type? GetEnumType(Type t)
    {
        if (t.IsEnum)
            return t;

        var t2 = Nullable.GetUnderlyingType(t);

        if (t2 is not { IsEnum: true })
            return null;

        return t2;
    }
}