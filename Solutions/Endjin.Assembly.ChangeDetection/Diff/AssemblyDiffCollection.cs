using System.Collections.Generic;
using System.Diagnostics;
using Mono.Cecil;

namespace Endjin.Assembly.ChangeDetection.Diff
{
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