using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

using ConsoleFx.Parser;
using ConsoleFx.Parser.Styles;
using ConsoleFx.Parser.Validators;
using ConsoleFx.Programs;
using ConsoleFx.Programs.UsageBuilders;

using RestGen;
using RestGen.Lang.Typescript;
using RestGen.Swagger;

namespace WebApiGen.SwaggerGen
{
    public sealed class Runner : SingleCommandProgram
    {
        public Uri SwaggerUrl { get; set; }
        public string SwaggerSource { get; set; }
        public string OutputFileName { get; set; } = "swagger.ts";
        public string ModelNs { get; set; } = null;
        public string ServiceNs { get; set; } = null;
        public string ModuleName { get; set; } = "common";
        public bool FixNames { get; set; }
        public List<string> References { get; } = new List<string>();

        public Runner() : base(new WindowsParserStyle())
        {
            UsageBuilder = new MetadataUsageBuilder();
        }

        protected override int Handle()
        {
            Uri swaggerUri;
            SwaggerInput input;
            if (Uri.TryCreate(SwaggerSource, UriKind.Absolute, out swaggerUri))
                input = new SwaggerInput(swaggerUri);
            else
                input = new SwaggerInput(File.ReadAllText(SwaggerSource));

            RestDefinition definition = input.GenerateDefinition();

            var generator = new AngularHttpGenerator(o => {
                o.Ns.Models = ModelNs;
                o.Ns.Interfaces = o.Ns.Implementations = ServiceNs;
                o.NgModule = ModuleName ?? "common";
                if (FixNames)
                    o.NameTransforms.MethodNames =
                        name => Regex.Replace(name, @"^(\w+)Using(GET|POST|PUT|DELETE|HEAD)$", "$1");
                foreach (string reference in References)
                    o.ReferencePaths.Add(reference);
            });
            string c = generator.Generate(definition);
            File.WriteAllText(OutputFileName, c);
            return 0;
        }

        protected override IEnumerable<Argument> GetArguments()
        {
            yield return CreateArgument()
                .Description("swagger url", "URL to the Swagger endpoint.")
                .ValidateWith(new FilePathOrUrlValidator())
                .AssignTo(() => SwaggerSource);
        }

        protected override IEnumerable<Option> GetOptions()
        {
            yield return CreateOption("out", "o")
                .Description("Path to the output file. If not specified, outputs to a file named swagger.ts in the current directory.")
                .Optional()
                .ExpectedParameters(1)
                .ValidateWith(new PathValidator(checkIfExists: false))
                .AssignTo(() => OutputFileName);
            yield return CreateOption("modelns", "mns")
                .Description("Typescript namespace for the model interfaces.")
                .ExpectedParameters(1)
                .AssignTo(() => ModelNs);
            yield return CreateOption("servicens", "sns")
                .Description("Typescript namespace for the service interfaces and classes.")
                .ParametersOptional()
                .AssignTo(() => ServiceNs);
            yield return CreateOption("module", "m")
                .Description("Name of Angular module to register service under.")
                .ExpectedParameters(1)
                .AssignTo(() => ModuleName);
            yield return CreateOption("def", "d")
                .Description("Absolute or relative path to a Typescript definition file.")
                .Optional(int.MaxValue)
                .ExpectedParameters(int.MaxValue)
                .AddToList(() => References);
            yield return CreateOption("fixnames", "fix")
                .Description("Fix common issues with generated named, such as redundant suffices")
                .Flag(() => FixNames);
        }
    }

    public sealed class FilePathOrUrlValidator : Validator<string>
    {
        protected override string PrimaryChecks(string parameterValue)
        {
            try
            {
                var uriValidator = new UriValidator(UriKind.Absolute);
                uriValidator.Validate(parameterValue);
                return parameterValue;
            } catch (ParserException ex) when (ex.ErrorCode == 1)
            {
                var pathValidator = new PathValidator();
                pathValidator.Validate(parameterValue);
                return parameterValue;
            }
        }
    }
}