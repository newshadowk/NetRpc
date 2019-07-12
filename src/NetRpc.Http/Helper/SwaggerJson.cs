using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using NJsonSchema;
using NSwag;

namespace NetRpc.Http
{
    public class SwaggerJson
    {
        private readonly string _apiRootPath;
        private readonly object[] _instances;

        public SwaggerJson(string apiRootPath, object[] instances)
        {
            _apiRootPath = apiRootPath;
            _instances = instances;
        }

        public string ToJson()
        {
            var doc = GetOpenApiDocument();
            return doc.ToJson();
        }

        public OpenApiDocument GetOpenApiDocument()
        {
            OpenApiDocument doc = new OpenApiDocument();
            (List<Type> paramTypes, List<(Type interfaceInstance, List<MethodInfo> methods)> list) = GetMethodInfos(_instances);

            //model
            paramTypes.ForEach(i => doc.Definitions.Add(i.Name, JsonSchema.FromType(i)));

            //tag
            list.ForEach(i => doc.Tags.Add(new OpenApiTag {Name = i.interfaceInstance.Name}));

            //path
            foreach (var tagGroup in list)
            {
                foreach (var method in tagGroup.methods)
                {
                    string key = $"{_apiRootPath}/{tagGroup.interfaceInstance.Name}/{method.Name}";
                    doc.Paths[key] = new OpenApiPathItem();
                    var operation = new OpenApiOperation();
                    operation.Tags.Add(tagGroup.interfaceInstance.Name);
                    operation.Responses.Add("200", GetOpenApiResponse(method.ReturnType));
                    foreach (var param in method.GetParameters())
                    {
                         var apiParam = GetOpenApiParameter(param.Name, param.ParameterType, true);
                         operation.Parameters.Add(apiParam);
                    }
                    doc.Paths[key]["post"] = operation;
                }
            }

            return doc;
        }

        private static (List<Type> paramTypes, List<(Type interfaceInstance, List<MethodInfo> methods)>) GetMethodInfos(object[] instances)
        {
            var list = new List<(Type interfaceInstance, List<MethodInfo> methods)>();
            var paramTypes = new List<Type>();

            foreach (var i in instances)
            {
                foreach (var m in GetMethodInfos(i))
                {
                    paramTypes.AddRange(m.paramTypes);
                    list.Add((m.interfaceInstance, m.methods));
                }
            }

            paramTypes = paramTypes.Distinct().ToList();
            return (paramTypes, list);
        }

        private static List<(Type interfaceInstance, List<Type> paramTypes, List<MethodInfo> methods)> GetMethodInfos(object instance)
        {
            var ret = new List<(Type interfaceInstance, List<Type> paramTypes, List<MethodInfo> methods)>();
            var instanceType = instance.GetType();
            foreach (var item in instanceType.GetInterfaces())
                ret.Add(GetMethodInfosByInterface(item));

            return ret;
        }

        private static (Type interfaceInstance, List<Type> paramTypes, List<MethodInfo> methods) GetMethodInfosByInterface(Type interfaceInstance)
        {
            var ms = interfaceInstance.GetMethods();
            var types = new List<Type>();
            ms.ToList().ForEach(i => types.AddRange(GetTypes(i)));
            return (interfaceInstance, types, ms.ToList());
        }

        private static List<Type> GetTypes(MethodInfo methodInfo)
        {
            var ret = new List<Type>();
            foreach (var p in methodInfo.GetParameters())
            {
                if (p.ParameterType == typeof(CancellationToken?) || p.ParameterType == typeof(CancellationToken))
                    continue;

                Type t;
                if (p.ParameterType.IsGenericType && p.ParameterType.GetGenericTypeDefinition() == typeof(Action<>))
                    t = p.ParameterType.GenericTypeArguments[0];
                else
                    t = p.ParameterType;

                if (!NetRpc.Helper.IsSystemType(t))
                    ret.Add(t);
            }

            return ret;
        }

        private static OpenApiResponse GetOpenApiResponse(Type type)
        {
            var retType = NetRpc.Helper.GetTypeFromReturnTypeDefinition(type);
            (JsonObjectType rType, OpenApiParameterKind _, string format) = GetJsonObjectTypeForParam(retType, false);
            OpenApiResponse ret = new OpenApiResponse();

            var hasStream = retType.HasStream();
            if (hasStream)
                ret.ActualResponse.Content.Add("application/octet-stream", new OpenApiMediaType {Schema = new JsonSchema {Type = rType, Format = format}});
            else
                ret.ActualResponse.Content.Add("application/json", new OpenApiMediaType {Schema = new JsonSchema {Type = rType, Format = format}});
            return ret;
        }

        private static (JsonObjectType type, OpenApiParameterKind kind, string format) GetJsonObjectTypeForParam(Type type, bool isGet)
        {
            OpenApiParameterKind kind;
            string format = null;
            JsonObjectType rType = JsonObjectType.String;

            if (isGet)
                kind = OpenApiParameterKind.Query;
            else
                kind = OpenApiParameterKind.Body;

            if (type == typeof(Guid))
            {
                format = JsonFormatStrings.Guid;
                rType = JsonObjectType.String;
            }
            else if (type == typeof(int) ||
                     type == typeof(short) ||
                     type == typeof(long) ||
                     type == typeof(uint) ||
                     type == typeof(ushort) ||
                     type == typeof(ulong))
            {
                format = JsonFormatStrings.Integer;
                rType = JsonObjectType.Integer;
            }
            else if (type == typeof(float) ||
                     type == typeof(double) ||
                     type == typeof(decimal))
            {
                format = JsonFormatStrings.Double;
                rType = JsonObjectType.Number;
            }
            else if (type == typeof(bool))
            {
                rType = JsonObjectType.Boolean;
            }
            else if (type == typeof(Stream))
            {
                rType = JsonObjectType.String;
                format = JsonFormatStrings.Binary;
                kind = OpenApiParameterKind.FormData;
            }

            return (rType, kind, format);
        }

        private static OpenApiParameter GetOpenApiParameter(string name, Type type, bool isGet)
        {
            (JsonObjectType rType, OpenApiParameterKind kind, string format) = GetJsonObjectTypeForParam(type, isGet);
            var p = new OpenApiParameter();
            p.Name = name;
            p.Kind = kind;
            p.IsRequired = true;
            p.Schema = new OpenApiParameter();
            p.Schema.Type = rType;
            p.Schema.Format = format;
            if (type == typeof(Stream))
            {
                p.Type = JsonObjectType.File;
            }
            return p;
        }
    }
}