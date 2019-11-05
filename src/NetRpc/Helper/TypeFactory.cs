using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace NetRpc
{
    public static class TypeFactory
    {
        private static volatile CacheTypeBuilder _builder = new CacheTypeBuilder();

        public static TypeInfo BuildType(string typeName, List<CustomsPropertyInfo> ps)
        {
            return _builder.BuildType(typeName, ps);
        }
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

    public class CacheTypeBuilder
    {
        private readonly ModuleBuilder _moduleBuilder;
        private readonly ConcurrentDictionary<string, TypeInfo> _dicType = new ConcurrentDictionary<string, TypeInfo>();

        public CacheTypeBuilder()
        {
            var myAsmName = new AssemblyName(Guid.NewGuid().ToString());
            var asmBuilder = AssemblyBuilder.DefineDynamicAssembly(myAsmName, AssemblyBuilderAccess.Run);
            _moduleBuilder = asmBuilder.DefineDynamicModule(myAsmName.Name);
        }

        public TypeInfo BuildType(string typeName, List<CustomsPropertyInfo> ps)
        {
            return _dicType.GetOrAdd(typeName,s => BuildTypeInner(s, ps));
        }

        public TypeInfo BuildTypeInner(string typeName, List<CustomsPropertyInfo> ps)
        {
            var typeBuilder = _moduleBuilder.DefineType(typeName,
                TypeAttributes.Public);

            AddPropertyToTypeBuilder(typeBuilder, ps);
            return typeBuilder.CreateTypeInfo();
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
    }
}