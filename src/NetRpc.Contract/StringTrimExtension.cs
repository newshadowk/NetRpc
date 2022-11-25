using System.Reflection;

namespace NetRpc.Contract;

public static class StringTrimExtension
{
    public static void StringTrim(this object obj)
    {
        foreach (var pi in obj.GetType().GetProperties())
        {
            if (pi.PropertyType != typeof(string))
                continue;

            var noTrim = pi.GetCustomAttribute<NoTrimAttribute>();
            if (noTrim != null)
                continue;

            var v = pi.GetValue(obj);
            if (v == null)
                continue;

            pi.SetValue(obj, ((string)v).Trim());
        }
    }
}