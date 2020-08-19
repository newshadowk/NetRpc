using System;
using System.Reflection;

namespace NetRpc
{
    public class PPInfo
    {
        public PropertyInfo? PropertyInfo { get; }

        public ParameterInfo? ParameterInfo { get; }

        public Type Type { get; }

        public string Name { get; }

        public PPInfo(PropertyInfo propertyInfo)
        {
            PropertyInfo = propertyInfo;
            Type = propertyInfo.PropertyType;
            Name = propertyInfo.Name;
        }

        public PPInfo(ParameterInfo parameterInfo)
        {
            ParameterInfo = parameterInfo;
            Type = parameterInfo.ParameterType;
            Name = parameterInfo.Name;
        }

        public PPInfo(string name, Type type)
        {
            Type = type;
            Name = name;
        }
    }
}