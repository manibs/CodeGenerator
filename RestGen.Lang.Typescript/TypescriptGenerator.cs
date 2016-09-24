using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RestGen.Lang.Typescript
{
    public abstract class TypescriptGenerator<TOptions> : Generator<TOptions>
        where TOptions : TypescriptGenerateOptions, new()
    {
        protected TypescriptGenerator(Action<TOptions> optionsSetter = null) : base(optionsSetter)
        {
        }

        public override string Generate(RestDefinition definition)
        {
            var code = new CodeBuilder();
            GenerateReferencePaths(code);
            GenerateInterfaces(code, definition);
            GenerateImplementations(code, definition);
            GenerateModels(code, definition.Models);
            return code.ToString();
        }

        private void GenerateReferencePaths(CodeBuilder code)
        {
            foreach (string referencePath in Options.ReferencePaths)
                code.Line($@"/// <reference path=""{referencePath}"" />");
            if (Options.ReferencePaths.Count > 0)
                code.Line();
        }

        protected abstract void GenerateInterfaces(CodeBuilder code, RestDefinition definition);

        protected abstract void GenerateImplementations(CodeBuilder code, RestDefinition definition);

        protected static void GenerateUrlBuilderFunction(CodeBuilder code)
        {
            using (code.Block("function buildServiceUrl(baseUrl: string, resourceUrl: string, queryParams?: any): string"))
            {
                code.Line("let url: string = baseUrl;");
                code.Line("let baseUrlSlash: boolean = url[url.length - 1] === '/';");
                code.Line("let resourceUrlSlash: boolean = resourceUrl[0] === '/';");
                using (code.Block("if (!baseUrlSlash && !resourceUrlSlash)"))
                    code.Line("url += '/';");
                using (code.Block("else if (baseUrlSlash && resourceUrlSlash)"))
                    code.Line("url = url.substr(0, url.length - 1);");
                code.Line("url += resourceUrl;");
                code.Line();
                using (code.Block("if (queryParams)"))
                {
                    code.Line("let isFirst: boolean = true;");
                    using (code.Block("for (let p in queryParams)"))
                    {
                        using (code.Block("if (queryParams.hasOwnProperty(p) && queryParams[p])"))
                        {
                            code.Line("let separator: string = isFirst ? '?' : '&';");
                            code.Line("url += `${separator}${p}=${encodeURI(queryParams[p])}`;");
                            code.Line("isFirst = false;");
                        }
                    }
                }
                code.Line("return url;");
            }
        }

        private void GenerateModels(CodeBuilder code, ModelDefinitions models)
        {
            Tuple<IDisposable, string> blockAndQualifier = GetBlockAndQualifier(code, Options.Ns.Models);

            using (blockAndQualifier.Item1)
            {
                foreach (ModelDefinition model in models.OrderBy(m => m.Name))
                {
                    using (code.Block($"{blockAndQualifier.Item2} interface {model.Name}"))
                    {
                        foreach (ModelPropertyDefinition property in model.Properties)
                        {
                            code.Code(property.Name);
                            //if (property.Requirement == Requirement.Optional)
                            //    code.Then("?");
                            code.Then($": {GetTypeSignature(property.Type)};").Line();
                        }
                    }
                }

                code.Line($"{blockAndQualifier.Item2} type Initializer<TModel> = (model: TModel) => void;");

                using (code.Block($"{blockAndQualifier.Item2} class ModelFactory"))
                    GenerateModelFactory(code, models);
            }
        }

        private static void GenerateModelFactory(CodeBuilder code, ModelDefinitions models)
        {
            foreach (ModelDefinition model in models)
            {
                using (code.Block($"public static createEmpty{model.Name}(initializer?: (model: {model.Name}) => void): {model.Name}"))
                {
                    using (code.BlockTerminated($"let model: {model.Name} = "))
                    {
                        var count = model.Properties.Count;
                        foreach (ModelPropertyDefinition property in model.Properties)
                        {
                            count--;
                            code.Code($"{property.Name}: ");
                            if (property.Type.IsCollection)
                                code.Then("[]");
                            else if (property.Type.IsComplex)
                                code.Then($"ModelFactory.createEmpty{property.Type.ComplexType}()");
                            else
                                code.Then(GetDefaultForPrimitiveType(property.Type.PrimitiveType));
                            if (count>0)
                                code.Then(",");
                            code.Line();
                        }
                    }
                    using (code.Block("if (!!initializer)"))
                        code.Line("initializer(model);");
                    code.Line("return model;");
                }
            }
        }

        private static string GetDefaultForPrimitiveType(Type primitiveType)
        {
            string primitiveTypeString = GetPrimitiveType(primitiveType);
            switch (primitiveTypeString)
            {
                case "string": return "''";
                case "number": return "undefined";
                case "boolean": return "false";
                case "Date": return "undefined";
                case "any": return "{}";
                default:
                    throw new Exception($"Cannot get a default value for primitive type {primitiveType}");
            }
        }

        protected static Tuple<IDisposable, string> GetBlockAndQualifier(CodeBuilder code, string ns)
        {
            return string.IsNullOrEmpty(ns) ? Tuple.Create(code.DummyBlock(), "public") : Tuple.Create(code.Block($"namespace {ns}"), "export");
        }

        protected string GetMethodSignature(OperationDefinition operation)
        {
            NameTransformer methodTransformer = Options?.NameTransforms?.MethodNames ?? (str => str);
            var sb = new StringBuilder(methodTransformer(operation.Name));
            sb.Append("(");
            for (int i = 0; i < operation.Parameters.Count; i++)
            {
                ParameterDefinition parameter = operation.Parameters[i];
                if (i > 0)
                    sb.Append(", ");
                sb.Append(parameter.Name);
                if (parameter.Requirement == Requirement.Optional)
                    sb.Append("?");
                sb.AppendFormat(": {0}", GetTypeSignature(parameter.Type, true));
                //TODO: Add defaults
            }
            sb.Append(")");
            sb.AppendFormat(": angular.IPromise<{0}>", GetTypeSignature(operation.SuccessResponseType, true));
            return sb.ToString();
        }

        protected string GetTypeSignature(DataType type, bool qualifyWithNs = false)
        {
            string signature;
            if (type.IsPrimitive)
                signature = GetPrimitiveType(type.PrimitiveType);
            else
                signature = qualifyWithNs ? QualifyWithNs(type.ComplexType, Options.Ns.Models) : type.ComplexType;
            if (type.IsCollection)
                signature += "[]";
            return signature;
        }

        protected static string QualifyWithNs(string str, string ns)
        {
            return string.IsNullOrEmpty(ns) ? str : ns + "." + str;
        }

        //TODO:Mani made changes to this
        private static string GetPrimitiveType(Type type)
        {
            const string objectErrorMessage = @"
An object type was found in the swagger json. This is not allowed.
If you are using ASP.NET Web API then ensure that all action methods that return IHttpActionResult or HttpResponseMessage types are decorated with a ResponseType attribute that specifies the actual return data type.
If you are using .NET, Java or any languages that have non-generic collections, ensure that you are using proper generic collection types and do not specify Object as the generic type parameter.
If you are using any other REST framework, ensure that all necessary type information is provided so that the Swagger json can be generated correctly.";

            string result;
            _typeMappings.TryGetValue(type, out result);
                return result;

            //TODO:Mani commented start
            //if (type == typeof(object)) 
            //    throw new Exception(objectErrorMessage);
            //throw new Exception($"Do not have a Typescript mapping for the {type.FullName} .NET type.");
            //TODO:Mani commented end
        }

        private static readonly Dictionary<Type, string> _typeMappings = new Dictionary<Type, string> {
            { typeof(string), "string" },
            { typeof(char), "string" },
            { typeof(int), "number" },
            { typeof(uint), "number" },
            { typeof(long), "number" },
            { typeof(ulong), "number" },
            { typeof(short), "number" },
            { typeof(ushort), "number" },
            { typeof(byte), "number" },
            { typeof(sbyte), "number" },
            { typeof(float), "number" },
            { typeof(double), "number" },
            { typeof(decimal), "number" },
            { typeof(bool), "boolean" },
            { typeof(DateTime), "Date" },
            { typeof(void), "void" },
        };
    }
}