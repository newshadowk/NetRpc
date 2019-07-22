using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Namotion.Reflection;
using NetRpc.Http.FaultContract;
using Newtonsoft.Json;
using NJsonSchema;
using NSwag;
using NSwag.Generation;

namespace NetRpc.Http
{
    internal class SwaggerJson
    {
        private readonly string _apiRootPath;
        private readonly object[] _instances;
        private readonly OpenApiDocument _doc;
        private readonly OpenApiDocumentGenerator _generator;
        private readonly OpenApiParameter _faultApiParam;
        private readonly OpenApiDocumentGeneratorSettings _gSetting;

        public SwaggerJson(string apiRootPath, object[] instances)
        {
            _apiRootPath = apiRootPath;
            _instances = instances;
            _doc = new OpenApiDocument();
            _doc.SchemaType = SchemaType.Swagger2;
            _gSetting = new OpenApiDocumentGeneratorSettings { Title = null, SchemaType = SchemaType.Swagger2 };
            _generator = new OpenApiDocumentGenerator(_gSetting, new OpenApiSchemaResolver(_doc, _gSetting));
            _faultApiParam = _generator.CreatePrimitiveParameter(null, null, typeof(Fault).ToContextualType());

            //_doc.Consumes.Add("text/plain");
            //_doc.Consumes.Add("application/json");

            //_doc.Produces.Add("text/plain");
            //_doc.Produces.Add("application/json");
        }

        public string ToJson()
        {
            Process();
            return _doc.ToJson();
        }

        private void Process()
        {
            var (paramTypes, list) = GetMethodInfos(_instances);

            //model
            //merge all param to one json obj for each method.
            paramTypes.ForEach(i => _generator.CreatePrimitiveParameter(i.Name, null, i.ToContextualType()));

            //tag
            list.ForEach(i => _doc.Tags.Add(new OpenApiTag { Name = i.interfaceInstance.Name }));

            //path
            foreach (var tagGroup in list)
            {
                foreach (var method in tagGroup.methods)
                {
                    var key = $"{_apiRootPath}/{tagGroup.interfaceInstance.Name}/{method.Name}";
                    _doc.Paths[key] = new OpenApiPathItem();
                    var operation = new OpenApiOperation();
                    operation.Tags.Add(tagGroup.interfaceInstance.Name);
       
                    SetOpenApiResponses(operation.Responses, method);
                    SetOpenApiParameters(operation, method);
                    _doc.Paths[key]["post"] = operation;
                }
            }
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
                var t = GetTypeByParamType(p.ParameterType);
                if (t != null)
                    ret.Add(t);
            }

            return ret;
        }

        private static Type GetTypeByParamType(Type paramType)
        {
            if (paramType == typeof(CancellationToken?) || paramType == typeof(CancellationToken))
                return null;

            Type t;
            if (paramType.IsGenericType && paramType.GetGenericTypeDefinition() == typeof(Action<>))
                t = paramType.GenericTypeArguments[0];
            else
                t = paramType;

            if (NetRpc.Helper.IsSystemType(t))
                return null;

            return t;
        }

        private void SetOpenApiResponses(IDictionary<string, OpenApiResponse> responses, MethodInfo method)
        {
            var returnType = Helper.GetTypeFromReturnTypeDefinition(method.ReturnType);

            //200
            var res200 = new OpenApiResponse();
            var hasStream = returnType.HasStream();
            res200.IsNullableRaw = false;
            if (hasStream)
            {
                res200.Description = "file";
                res200.Schema = new JsonSchema
                {
                    Type = JsonObjectType.File
                };
            }
            else
            {
                var apiP = _generator.CreatePrimitiveParameter(null, null, returnType.ToContextualType());
                res200.Schema = new JsonSchema();
                res200.Schema.Reference = apiP;
            }

            responses.Add("200", res200);

            //400
            var faultContractTypes = GetFaultContractTypes(method);
            if (faultContractTypes.Count == 0)
                return;

            var res400 = new OpenApiResponse();
            res400.IsNullableRaw = false;
            res400.Schema = new JsonSchema();
            res400.Schema.Reference = _faultApiParam;
            res400.Description += "<b>Name</b> : Name of the exception.<br/><b>Details</b> : Maybe one of model below:";
            foreach (var detailsType in faultContractTypes)
            {
                _generator.CreatePrimitiveParameter(null, null, detailsType.ToContextualType());
                res400.Description += $"<br/> <b>{detailsType.Name}</b>";
            }

            responses.Add("400", res400);
        }

        private void SetOpenApiParameters(OpenApiOperation operation, MethodInfo method)
        {
            var argType = Helper.GetArgType(method, out var streamName, out var action, out var cancelToken);
            if (argType != null)
                operation.Parameters.Add(GetOpenApiParameter("data", argType, streamName != null));

            if (streamName != null)
            {
                var p = new OpenApiParameter();
                p.Name = streamName;
                p.Schema = new JsonSchema();
                p.Type = JsonObjectType.File;
                p.Kind = OpenApiParameterKind.FormData;
                operation.Parameters.Add(p);
                operation.Consumes = new List<string>();
                operation.Consumes.Add("multipart/form-data");//todo
            }

            if (action != null)
            {
                var actionArg = action.Type.GetGenericArguments()[0];
                var p = GetOpenApiParameter(action.Name, actionArg);
                p.Description = "Callback type. (Leave this field blank)";
                p.IsRequired = false;
                p.Schema.Default = null;
                operation.Parameters.Add(p);
            }

            if (cancelToken != null)
            {
                var p = GetOpenApiParameter(cancelToken.Name, typeof(object));
                p.Description = "CancelToken. (Leave this field blank)";
                p.IsRequired = false;
                p.Default = null;
                operation.Parameters.Add(p);
            }
        }

        private static List<Type> GetFaultContractTypes(MethodInfo method)
        {
            var ret = new List<Type>();
            foreach (SwaggerFaultContractAttribute a in method.GetCustomAttributes(typeof(SwaggerFaultContractAttribute), true))
                ret.Add(a.DetailType);
            return ret;
        }

        private OpenApiParameter GetOpenApiParameter(string name, Type type, bool isFormData = false)
        {
            var p = new OpenApiParameter();
            p.Name = name;

            if (isFormData)
                p.Kind = OpenApiParameterKind.FormData;
            else
                p.Kind = OpenApiParameterKind.Body;

            if (NetRpc.Helper.IsSystemType(type))
            {
                p.Schema = JsonSchema.FromType(type, _gSetting);
                return p;
            }
        
            p.Schema = new JsonSchema();
            var genP = _generator.CreatePrimitiveParameter(null, null, type.ToContextualType());
            p.Schema.Reference = genP;

            if (isFormData)
            {
                var instance = Activator.CreateInstance(type);
                instance.SetDefaultValue();
                var defaultJson = JsonConvert.SerializeObject(instance);
                p.Default = defaultJson;
                p.Description = $"Model:<b>{type.Name}</b>";
            }

            return p;
        }
    }
}