namespace AssemblyDifferences.Diff
{
    #region Using Directives

    using System;
    using System.Collections.Generic;

    using AssemblyDifferences.Introspection;
    using AssemblyDifferences.Query;

    using Mono.Cecil;

    #endregion

    public class AssemblyDiffer
    {
        private readonly AssemblyDiffCollection myDiff = new AssemblyDiffCollection();

        private readonly AssemblyDefinition myV1;

        private readonly AssemblyDefinition myV2;

        public AssemblyDiffer(AssemblyDefinition v1, AssemblyDefinition v2)
        {
            if (v1 == null)
            {
                throw new ArgumentNullException("v1");
            }
            if (v2 == null)
            {
                throw new ArgumentNullException("v2");
            }

            this.myV1 = v1;
            this.myV2 = v2;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="AssemblyDiffer" /> class.
        /// </summary>
        /// <param name="assemblyFileV1">The assembly file v1.</param>
        /// <param name="assemblyFileV2">The assembly file v2.</param>
        public AssemblyDiffer(string assemblyFileV1, string assemblyFileV2)
        {
            if (string.IsNullOrEmpty(assemblyFileV1))
            {
                throw new ArgumentNullException("assemblyFileV1");
            }
            if (string.IsNullOrEmpty(assemblyFileV2))
            {
                throw new ArgumentNullException("assemblyFileV2");
            }

            this.myV1 = AssemblyLoader.LoadCecilAssembly(assemblyFileV1);
            if (this.myV1 == null)
            {
                throw new ArgumentException(string.Format("Could not load assemblyV1 {0}", assemblyFileV1));
            }

            this.myV2 = AssemblyLoader.LoadCecilAssembly(assemblyFileV2);
            if (this.myV2 == null)
            {
                throw new ArgumentException(string.Format("Could not load assemblyV2 {0}", assemblyFileV2));
            }
        }

        private void OnAddedType(TypeDefinition type)
        {
            var diff = new DiffResult<TypeDefinition>(type, new DiffOperation(true));
            this.myDiff.AddedRemovedTypes.Add(diff);
        }

        private void OnRemovedType(TypeDefinition type)
        {
            var diff = new DiffResult<TypeDefinition>(type, new DiffOperation(false));
            this.myDiff.AddedRemovedTypes.Add(diff);
        }

        public AssemblyDiffCollection GenerateTypeDiff(QueryAggregator queries)
        {
            if (queries == null || queries.TypeQueries.Count == 0)
            {
                throw new ArgumentNullException("queries is null or contains no queries");
            }

            var typesV1 = queries.ExeuteAndAggregateTypeQueries(this.myV1);
            var typesV2 = queries.ExeuteAndAggregateTypeQueries(this.myV2);

            var differ = new ListDiffer<TypeDefinition>(this.ShallowTypeComapare);

            differ.Diff(typesV1, typesV2, this.OnAddedType, this.OnRemovedType);

            this.DiffTypes(typesV1, typesV2, queries);

            return this.myDiff;
        }

        private bool ShallowTypeComapare(TypeDefinition v1, TypeDefinition v2)
        {
            return v1.FullName == v2.FullName;
        }

        private void DiffTypes(List<TypeDefinition> typesV1, List<TypeDefinition> typesV2, QueryAggregator queries)
        {
            TypeDefinition typeV2;
            foreach (var typeV1 in typesV1)
            {
                typeV2 = this.GetTypeByDefinition(typeV1, typesV2);
                if (typeV2 != null)
                {
                    var diffed = TypeDiff.GenerateDiff(typeV1, typeV2, queries);
                    if (TypeDiff.None != diffed)
                    {
                        this.myDiff.ChangedTypes.Add(diffed);
                    }
                }
            }
        }

        private TypeDefinition GetTypeByDefinition(TypeDefinition search, List<TypeDefinition> types)
        {
            foreach (var type in types)
            {
                if (type.IsEqual(search))
                {
                    return type;
                }
            }

            return null;
        }
    }
}