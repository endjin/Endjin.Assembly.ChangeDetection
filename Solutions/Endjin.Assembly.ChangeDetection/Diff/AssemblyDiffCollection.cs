namespace AssemblyDifferences.Diff
{
    using System.Collections.Generic;
    using System.Diagnostics;

    using Mono.Cecil;

    [DebuggerDisplay("Add {AddedRemovedTypes.AddedCount} Remove {AddedRemovedTypes.RemovedCount} Changed {ChangedTypes.Count}")]
    public class AssemblyDiffCollection
    {
        public DiffCollection<TypeDefinition> AddedRemovedTypes;

        public List<TypeDiff> ChangedTypes;

        public AssemblyDiffCollection()
        {
            this.AddedRemovedTypes = new DiffCollection<TypeDefinition>();
            this.ChangedTypes = new List<TypeDiff>();
        }
    }
}