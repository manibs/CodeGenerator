using System;

namespace RestGen
{
    public abstract class Generator
    {
        public abstract string Generate(RestDefinition definition);
    }

    public abstract class Generator<TOptions> : Generator
        where TOptions : GenerateOptions, new()
    {
        protected Generator(Action<TOptions> optionsSetter = null)
        {
            optionsSetter?.Invoke(Options);
        }

        public TOptions Options { get; } = new TOptions();
    }
}