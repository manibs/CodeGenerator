using System;
using System.IO;
using System.Text.RegularExpressions;

using RestGen.Lang.Typescript;
using RestGen.Swagger;

using Xunit;

namespace RestGen.Tests
{
    public sealed class SwaggerTests
    {
        [Theory]
        [InlineData("swagger2.json")]
        public void TestSwaggerFile(string filename)
        {
            string swaggerJson = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), filename));
            var input = new SwaggerInput(swaggerJson);

            GenerateCode(input);
        }

        [Theory]
        [InlineData("http://localhost/spm.services/swagger/docs/v1")]
        public void TestSwaggerUrl(string url)
        {
            var input = new SwaggerInput(new Uri(url));

            GenerateCode(input);
        }

        private static void GenerateCode(Input input)
        {
            RestDefinition definition = input.GenerateDefinition();
            Assert.NotNull(definition);

            var generator = new AngularHttpGenerator(
                o =>
                {
                    o.Ns.Models = "my.model";
                    o.Ns.Interfaces = "my.service";
                    o.Ns.Implementations = "my.service";
                    o.ReferencePaths.Add("../../../typings/lib.d.ts");
                    o.ReferencePaths.Add("../../../typings/app.d.ts");
                    o.NgModule = "blah";
                    o.NameTransforms.MethodNames = str =>
                    {
                        var pattern = new Regex(@"^(\w+)Using(GET|POST|PUT|DELETE|HEAD)$");
                        return pattern.Replace(str, "$1");
                    };
                });
            string code = generator.Generate(definition);
            Assert.NotNull(code);
            Assert.NotEmpty(code);
        }
    }
}
