namespace RestGen.Lang.Typescript
{
    public class AngularHttpGeneratorOptions : TypescriptGenerateOptions
    {
        public string NgModule { get; set; }
        public InjectionApproach InjectionApproach { get; set; }

    }

    public enum InjectionApproach
    {
        InjectArray,
        Annotation,
    }
}