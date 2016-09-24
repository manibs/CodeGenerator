using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;

using Newtonsoft.Json.Linq;

namespace RestGen.Swagger
{
    public sealed class SwaggerInput : Input
    {
        private readonly string _swaggerJson;

        public SwaggerInput(string swaggerJson)
        {
            if (swaggerJson == null)
                throw new ArgumentNullException(nameof(swaggerJson));
            _swaggerJson = swaggerJson;
        }

        public SwaggerInput(Uri swaggerUri)
        {
            if (swaggerUri == null)
                throw new ArgumentNullException(nameof(swaggerUri));
            var client = new HttpClient();
            _swaggerJson = client.GetStringAsync(swaggerUri).Result;
        }

        public override RestDefinition GenerateDefinition()
        {
            JObject swagger = JObject.Parse(_swaggerJson);

            var result = new RestDefinition();

            JEnumerable<JProperty> paths = swagger["paths"].Children<JProperty>();
            foreach (JProperty path in paths)
            {
                JEnumerable<JProperty> operations = path.Value.Children<JProperty>();
                foreach (JProperty operation in operations)
                {
                    string verb = operation.Name;
                    var tags = operation.Value["tags"].Value<JArray>();
                    JToken firstTag = tags.FirstOrDefault();
                    string serviceName = (firstTag?.Value<string>() ?? "Default").ToIdentifier().ToPascalCase();

                    ServiceDefinition service = result.Services[serviceName];
                    if (service == null)
                    {
                        service = new ServiceDefinition(serviceName);
                        result.Services.Add(service);
                    }

                    service.Operations.Add(GetOperationDefinition(verb, path, (JObject)operation.Value, result.Models));
                }
            }

            return result;
        }

        private static OperationDefinition GetOperationDefinition(string verb, JProperty path, JObject operation,
            ModelDefinitions modelDefinitions)
        {
            var name = operation["operationId"].Value<string>();

            var description = operation["description"]?.Value<string>();

            //TODO: Handle return types a bit more generically
            DataType returnType;
            JToken returnToken = operation["responses"]["200"];
            if (returnToken != null)
            {
                returnToken = returnToken["schema"];
                returnType = GetComplexDataType(returnToken, modelDefinitions);
            }
            else
                returnType = new DataType(typeof(string), false);

            var operationDef = new OperationDefinition(verb.ToUpperInvariant(), path.Name, name, description, returnType,
                null);
            IEnumerable<JObject> parameters = operation["parameters"]?.Values<JObject>();
            if (parameters != null)
            {
                foreach (JObject parameter in parameters)
                    operationDef.Parameters.Add(GetParameterDefinition(parameter, modelDefinitions));
            }
            return operationDef;
        }

        private static ParameterDefinition GetParameterDefinition(JObject parameter, ModelDefinitions modelDefinitions)
        {
            string name = parameter["name"].Value<string>().Replace(".", string.Empty);
            string @default = parameter["default"]?.Value<string>();

            var @in = parameter["in"].Value<string>();
            ParameterLocation location;
            if (!Enum.TryParse(@in, true, out location))
                location = ParameterLocation.Path;

            var required = parameter["required"].Value<bool>();
            Requirement requirement = required ? Requirement.Mandatory : Requirement.Optional;

            DataType type;
            JToken typeToken = parameter["type"];
            if (typeToken != null)
                type = GetPrimitiveDataType(typeToken, parameter["format"], false);
            else
            {
                JToken schemaToken = parameter["schema"];
                if (schemaToken == null)
                    throw new Exception($"Could not find type information about parameter '{name}'");
                type = GetComplexDataType(schemaToken, modelDefinitions);
            }

            var parameterDef = new ParameterDefinition(name, location, requirement, type, @default);
            return parameterDef;
        }

        private static DataType GetPrimitiveDataType(JToken typeToken, JToken formatToken, bool isArray)
        {
            var type = typeToken.Value<string>();
            Dictionary<string, Type> subTypeMappings;
            if (!_primitiveTypeMappings.TryGetValue(type, out subTypeMappings))
                throw new Exception($"No mapping for primitive type '{type}'");

            string format = formatToken != null ? (formatToken.Value<string>() ?? string.Empty) : string.Empty;
            Type primitiveType;
            if (format == "uuid")
            {
                format = string.Empty;
            }

            if (!subTypeMappings.TryGetValue(format, out primitiveType))
                throw new Exception($"No mapping for format '{format}' of type '{type}'");

            return new DataType(primitiveType, isArray);
        }

        private static readonly Dictionary<string, Dictionary<string, Type>> _primitiveTypeMappings =
            new Dictionary<string, Dictionary<string, Type>>
            {
                ["string"] = new Dictionary<string, Type>
                {
                    [string.Empty] = typeof(string),
                    ["byte"] = typeof(byte),
                    ["date"] = typeof(DateTime),
                    ["date-time"] = typeof(DateTime)
                },
                ["integer"] = new Dictionary<string, Type>
                {
                    [string.Empty] = typeof(long),
                    ["int32"] = typeof(int),
                    ["int64"] = typeof(long),
                    ["double"] = typeof(double),
                    ["float"] = typeof(float)
                },
                ["number"] = new Dictionary<string, Type>
                {
                    [string.Empty] = typeof(long),
                    ["int32"] = typeof(int),
                    ["int64"] = typeof(long),
                    ["double"] = typeof(double),
                    ["float"] = typeof(float)
                },
                ["boolean"] = new Dictionary<string, Type>
                {
                    [string.Empty] = typeof(bool)
                },
                ["file"] = new Dictionary<string, Type>
                {
                    [string.Empty] = typeof(byte[])
                },
                ["object"] = new Dictionary<string, Type>
                {
                    [string.Empty] = typeof(object)
                }
            };

        private static DataType GetComplexDataType(JToken schemaToken, ModelDefinitions modelDefinitions)
        {
            //Single complex type
            JToken refToken = schemaToken["$ref"];
            if (refToken != null)
                return LookupModel(refToken, modelDefinitions, false);

            JToken typeToken = schemaToken["type"];
            JToken formatToken;
            if (typeToken == null)
                throw new Exception("No useful information found in the schema element");

            if (typeToken.Value<string>().Equals("array"))
            {
                //Array of primitive or complex types

                JToken itemsToken = schemaToken["items"];

                //Array of complex type
                refToken = itemsToken["$ref"];
                if (refToken != null)
                    return LookupModel(refToken, modelDefinitions, true);

                //Array of primitive type
                typeToken = itemsToken["type"];
                formatToken = itemsToken["format"];
                return GetPrimitiveDataType(typeToken, formatToken, true);
            }

            formatToken = schemaToken["format"];
            return GetPrimitiveDataType(typeToken, formatToken, false);
        }

        private static DataType LookupModel(JToken refToken, ModelDefinitions modelDefinitions, bool isArray)
        {
            //var refPath = refToken.Value<string>().Substring(2).Replace('/', '.');
            string refPath = refToken.Value<string>().Split('/')
                .SkipWhile(piece => piece.Equals("#"))
                .Aggregate(new StringBuilder(), (sb, piece) => sb.Append($@"['{piece}']"))
                .ToString();
            JToken modelToken = refToken.Root.SelectToken(refPath);
            if (modelToken == null)
                throw new Exception($"Cannot find model definition for {refToken}");

            string name = ((JProperty)modelToken.Parent).Name
                .Replace("[", string.Empty)
                .Replace("]", string.Empty)
                .Replace(".", string.Empty);
            if (modelDefinitions.Contains(name))
                return new DataType(name, isArray);

            var modelDefinition = new ModelDefinition(name);
            modelDefinitions.Add(modelDefinition);

            JToken requiredToken = modelToken["required"];
            IEnumerable<string> requiredProperties = requiredToken != null
                ? requiredToken.Values<string>()
                : Enumerable.Empty<string>();

            JToken propertiesToken = modelToken["properties"];
            if (propertiesToken != null)
            {
                JEnumerable<JProperty> properties = propertiesToken.Children<JProperty>();
                foreach (JProperty property in properties)
                {
                    DataType type;

                    JToken propertyRefToken = property.Value["$ref"];
                    if (propertyRefToken != null)
                        type = GetComplexDataType(property.Value, modelDefinitions);
                    else
                    {
                        JToken typeToken = property.Value["type"];
                        JToken formatToken = property.Value["format"];

                        if (formatToken != null || !typeToken.Value<string>().Equals("array"))
                            type = GetPrimitiveDataType(typeToken, formatToken, false);
                        else
                            type = GetComplexDataType(property.Value, modelDefinitions);
                    }

                    Requirement requirement = requiredProperties.Any(p => p.Equals(property.Name))
                        ? Requirement.Mandatory
                        : Requirement.Optional;

                    var propertyDefinition = new ModelPropertyDefinition(property.Name, type, requirement);
                    modelDefinition.Properties.Add(propertyDefinition);
                }
            }

            return new DataType(name, isArray);
        }
    }
}
