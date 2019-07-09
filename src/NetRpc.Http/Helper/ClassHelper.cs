using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace NetRpc.Http
{
    public static class ClassHelper
    {
        public static object GetDefault(Type type)
        {
            if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }
            return null;
        }

        public static object CreateInstance(Type t)
        {
            return Activator.CreateInstance(t);
        }

        public static object CreateInstance(string className, List<CustomsPropertyInfo> ps)
        {
            var t = BuildType(className);
            t = AddProperty(t, ps);
            return Activator.CreateInstance(t);
        }

        public static object CreateInstance(List<CustomsPropertyInfo> ps)
        {
            return CreateInstance("DefaultClass", ps);
        }

        public static T InvokeMethod<T>(object classInstance, string methodName, params object[] args)
        {
            // ReSharper disable once PossibleNullReferenceException
            return (T)classInstance.GetType().GetMethod(methodName).Invoke(classInstance, args);
        }

        public static object InvokeMethod(object classInstance, string methodName, params object[] args)
        {
            // ReSharper disable once PossibleNullReferenceException
            return classInstance.GetType().GetMethod(methodName).Invoke(classInstance, args);
        }

        public static void SetPropertyValue(object classInstance, PropertyInfo tgtProperty, object propertyValue)
        {
            var type = classInstance.GetType();

            if (tgtProperty.PropertyType.IsEnum)
                propertyValue = Enum.ToObject(tgtProperty.PropertyType, propertyValue);

            if (propertyValue == DBNull.Value || propertyValue == null)
            {
                type.InvokeMember(tgtProperty.Name, BindingFlags.SetProperty, Type.DefaultBinder, classInstance, new object[] { null });
            }
            else
            {
                type.InvokeMember(tgtProperty.Name, BindingFlags.SetProperty, Type.DefaultBinder, classInstance,
                    new[] { Convert.ChangeType(propertyValue, tgtProperty.PropertyType) });
            }
        }

        public static object GetPropertyValue(object classInstance, string propertyName)
        {
            return classInstance.GetType().InvokeMember(propertyName, BindingFlags.GetProperty,
                null, classInstance, new object[] { });
        }

        public static Type BuildType(string className = "DefaultClass")
        {
            var myAsmName = new AssemblyName();
            myAsmName.Name = "MyDynamicAssembly";

            var myAsmBuilder = AssemblyBuilder.DefineDynamicAssembly(myAsmName,
                AssemblyBuilderAccess.RunAndCollect);


            var myModBuilder = myAsmBuilder.DefineDynamicModule(myAsmName.Name);
            var myTypeBuilder = myModBuilder.DefineType(className,
                TypeAttributes.Public);

            Type ret = myTypeBuilder.CreateTypeInfo();
            return ret;
        }

        public static Type AddProperty(Type classType, List<CustomsPropertyInfo> ps)
        {
            MergeProperty(classType, ps);
            return AddPropertyToType(classType, ps);
        }

        public static Type AddProperty(Type classType, CustomsPropertyInfo p)
        {
            var ps = new List<CustomsPropertyInfo>();
            ps.Add(p);
            MergeProperty(classType, ps);
            return AddPropertyToType(classType, ps);
        }

        public static Type DeleteProperty(Type classType, string propertyName)
        {
            var ls = new List<string>();
            ls.Add(propertyName);

            var ps = SeparateProperty(classType, ls);
            return AddPropertyToType(classType, ps);
        }

        public static Type DeleteProperty(Type classType, List<string> propertyNames)
        {
            var ps = SeparateProperty(classType, propertyNames);
            return AddPropertyToType(classType, ps);
        }

        private static void MergeProperty(Type t, List<CustomsPropertyInfo> ps)
        {
            foreach (var pi in t.GetProperties())
            {
                var cpi = new CustomsPropertyInfo(pi.PropertyType, pi.Name);
                ps.Add(cpi);
            }
        }

        private static List<CustomsPropertyInfo> SeparateProperty(Type t, List<string> propertyNames)
        {
            var ret = new List<CustomsPropertyInfo>();
            foreach (var pi in t.GetProperties())
            {
                foreach (var s in propertyNames)
                {
                    if (pi.Name != s)
                    {
                        var cpi = new CustomsPropertyInfo(pi.PropertyType, pi.Name);
                        ret.Add(cpi);
                    }
                }
            }

            return ret;
        }

        private static void AddPropertyToTypeBuilder(TypeBuilder myTypeBuilder, List<CustomsPropertyInfo> ps)
        {
            var getSetAttr = MethodAttributes.Public | MethodAttributes.SpecialName |
                             MethodAttributes.HideBySig;

            foreach (var cpi in ps)
            {
                var fieldNameBuilder = myTypeBuilder.DefineField(cpi.FieldName,
                    cpi.Type,
                    FieldAttributes.Private);

                var propertyNameBuilder = myTypeBuilder.DefineProperty(cpi.PropertyName,
                    PropertyAttributes.HasDefault,
                    cpi.Type,
                    null);


                var getPropertyMethodNameBuilder = myTypeBuilder.DefineMethod(cpi.GetPropertyMethodName,
                    getSetAttr,
                    cpi.Type,
                    Type.EmptyTypes);

                var getPropertyMethodNameBuilderGenerator = getPropertyMethodNameBuilder.GetILGenerator();
                getPropertyMethodNameBuilderGenerator.Emit(OpCodes.Ldarg_0);
                getPropertyMethodNameBuilderGenerator.Emit(OpCodes.Ldfld, fieldNameBuilder);
                getPropertyMethodNameBuilderGenerator.Emit(OpCodes.Ret);

                var setPropertyMethodNameBuilder = myTypeBuilder.DefineMethod(cpi.SetPropertyMethodName,
                    getSetAttr,
                    null,
                    new[] { cpi.Type });

                var setPropertyMethodNameBuilderGenerator = setPropertyMethodNameBuilder.GetILGenerator();
                setPropertyMethodNameBuilderGenerator.Emit(OpCodes.Ldarg_0);
                setPropertyMethodNameBuilderGenerator.Emit(OpCodes.Ldarg_1);
                setPropertyMethodNameBuilderGenerator.Emit(OpCodes.Stfld, fieldNameBuilder);
                setPropertyMethodNameBuilderGenerator.Emit(OpCodes.Ret);

                propertyNameBuilder.SetGetMethod(getPropertyMethodNameBuilder);
                propertyNameBuilder.SetSetMethod(setPropertyMethodNameBuilder);
            }
        }

        public static Type AddPropertyToType(Type classType, List<CustomsPropertyInfo> ps)
        {
            var myAsmName = new AssemblyName();
            myAsmName.Name = "MyDynamicAssembly";

            var myAsmBuilder = AssemblyBuilder.DefineDynamicAssembly(myAsmName,
                AssemblyBuilderAccess.RunAndCollect);


            var myModBuilder =
                myAsmBuilder.DefineDynamicModule(myAsmName.Name);

            var myTypeBuilder = myModBuilder.DefineType(classType.FullName,
                TypeAttributes.Public);

            AddPropertyToTypeBuilder(myTypeBuilder, ps);

            Type typeInfo = myTypeBuilder.CreateTypeInfo();

            return typeInfo;
        }

        public class CustomsPropertyInfo
        {
            public CustomsPropertyInfo()
            {
            }

            public CustomsPropertyInfo(Type type, string propertyName)
            {
                Type = type;
                PropertyName = propertyName;
            }

            public Type Type { get; set; }

            public string PropertyName { get; set; }

            public string FieldName => $"_{PropertyName.Substring(0, 1).ToLower()}{PropertyName.Substring(1)}";

            public string SetPropertyMethodName => "set_" + PropertyName;

            public string GetPropertyMethodName => "get_" + PropertyName;
        }
    }
}