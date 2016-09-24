namespace RestGen
{
    public class GenerateOptions
    {
    }

    public sealed class NameTransformOptions
    {
        public NameTransformer ServiceNames { get; set; }
        public NameTransformer MethodNames { get; set; }
    }

    public delegate string NameTransformer(string name);

    public static class NameTransform
    {
        public static readonly NameTransformer PascalCase = str => str.ToPascalCase();
        public static readonly NameTransformer CamelCase = str => str.ToCamelCase();
    }

    public sealed class NamespaceOptions
    {
        public string Models { get; set; }
        public string Interfaces { get; set; }
        public string Implementations { get; set; }

        public void SetAll(string ns)
        {
            Models = Interfaces = Implementations = ns;
        }
    }
}