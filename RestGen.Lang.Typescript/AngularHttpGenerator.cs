using System;
using System.Collections.Generic;
using System.Linq;

namespace RestGen.Lang.Typescript
{
    public sealed class AngularHttpGenerator : TypescriptGenerator<AngularHttpGeneratorOptions>
    {
        public AngularHttpGenerator(Action<AngularHttpGeneratorOptions> optionsSetter = null) : base(optionsSetter)
        {
        }

        protected override void GenerateInterfaces(CodeBuilder code, RestDefinition definition)
        {
            Tuple<IDisposable, string> blockAndQualifier = GetBlockAndQualifier(code, Options.Ns.Interfaces);

            using (blockAndQualifier.Item1)
            {
                foreach (ServiceDefinition service in definition.Services.OrderBy(sd => sd.Name))
                {
                    using (code.Block($"{blockAndQualifier.Item2} interface I{service.Name}WebService"))
                    {
                        foreach (OperationDefinition operation in service.Operations)
                            code.Line(GetMethodSignature(operation) + ";");
                    }
                }
            }
        }

        protected override void GenerateImplementations(CodeBuilder code, RestDefinition definition)
        {
            Tuple<IDisposable, string> blockAndQualifier = GetBlockAndQualifier(code, Options.Ns.Implementations);

            using (blockAndQualifier.Item1)
            {
                foreach (ServiceDefinition service in definition.Services.OrderBy(sd => sd.Name))
                {
                    string qualifiedInterfaceName = QualifyWithNs("I" + service.Name + "WebService", Options.Ns.Interfaces);
                    using (code.Block($"{blockAndQualifier.Item2} class {service.Name}WebService implements {qualifiedInterfaceName}"))
                    {
                        if (Options.InjectionApproach == InjectionApproach.Annotation)
                            code.Line("/* @ngInject */");
                        else
                            code.Line("public static $inject: string[] = ['$http', '$q', 'config'];");
                        using (code.Block("constructor (private $http: angular.IHttpService, private $q: angular.IQService, private config: common.config.IConfig)"))
                        {
                        }

                        foreach (OperationDefinition operation in service.Operations)
                        {
                            using (code.Block($"public {GetMethodSignature(operation)}"))
                            {
                                //Code to build the relative URL with the resource parameters.
                                code.Code($"let resourceUrl: string = '{operation.Path}'");
                                foreach (ParameterDefinition parameter in operation.Parameters.Where(p => p.Location == ParameterLocation.Path))
                                    code.Then($".replace('{{{parameter.Name}}}', {parameter.Name}.toString())");
                                code.Then(";").Line();

                                //Code to add any query string parameters to the end of the relative URL.
                                using (code.BlockTerminated("let queryParams: any ="))
                                {
                                    List<ParameterDefinition> queryParameters =
                                        operation.Parameters.Where(p => p.Location == ParameterLocation.Query).ToList();
                                    for (int i = 0; i < queryParameters.Count; i++)
                                    {
                                        ParameterDefinition parameter = queryParameters[i];
                                        code.Code($" {parameter.Name}: {parameter.Name}");
                                        if (i < queryParameters.Count - 1)
                                            code.Then(",");
                                        code.Line();
                                    }
                                }

                                string successType = GetTypeSignature(operation.SuccessResponseType, true);
                                code.Line($"return new this.$q<{successType}>(");
                                using (code.Indent())
                                {
                                    using (code.Block($"(resolve: angular.IQResolveReject<{successType}>, reject: angular.IQResolveReject<any>) =>"))
                                    {
                                        List<ParameterDefinition> formDataParameters = operation.Parameters.Where(p => p.Location == ParameterLocation.FormData).ToList();
                                        if (formDataParameters.Count > 0)
                                        {
                                            code.Line("let fd: FormData = new FormData();");
                                            foreach (ParameterDefinition parameter in formDataParameters)
                                                code.Line($"fd.append('{parameter.Name}', {parameter.Name});");
                                        }

                                        using (code.Block($"this.$http<{successType}>("))
                                        {
                                            code.Line($"method: '{operation.Verb.ToUpperInvariant()}',");

                                            if (formDataParameters.Count > 0)
                                            {
                                                code.Line("data: fd,");
                                                code.Line("headers: { 'Content-Type': undefined },");
                                                code.Line("transformRequest: angular.identity,");
                                            } else
                                            {
                                                ParameterDefinition bodyParameter = operation.Parameters.FirstOrDefault(
                                                    p => p.Location == ParameterLocation.Body);
                                                if (bodyParameter != null)
                                                    code.Line($"data: {bodyParameter.Name},");
                                            }
                                            code.Line("url: buildServiceUrl(this.config.apiBaseUrl, resourceUrl, queryParams)");
                                        }
                                        using (code.Block($").success((data: {successType}, status: number, headers: angular.IHttpHeadersGetter, config: angular.IRequestConfig) =>"))
                                            code.Line("resolve(data);");
                                        using (code.Block(").error((data: any, status: number, headers: angular.IHttpHeadersGetter, config: angular.IRequestConfig) =>"))
                                            code.Line("reject(data);");
                                        code.Line(");");
                                    }
                                }
                                code.Line(");");
                            }
                        }
                    }

                    code.Line($"angular.module('app').service('{service.Name.ToCamelCase()}WebService', {service.Name}WebService);");
                }

                GenerateUrlBuilderFunction(code);
            }
        }
    }
}