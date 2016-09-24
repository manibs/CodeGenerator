using System.Collections.Generic;
using System.Diagnostics;

namespace RestGen.Lang.Typescript
{
    public class TypescriptGenerateOptions : GenerateOptions
    {
        private readonly List<string> _referencePaths = new List<string>();

        public NamespaceOptions Ns { get; } = new NamespaceOptions();

        public NameTransformOptions NameTransforms { get; } = new NameTransformOptions();

        public IList<string> ReferencePaths
        {
            [DebuggerStepThrough]
            get { return _referencePaths; }
        }
    }
}
