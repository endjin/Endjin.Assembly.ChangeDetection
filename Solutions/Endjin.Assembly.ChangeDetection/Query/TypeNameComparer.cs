namespace AssemblyDifferences.Query
{
    using System.Collections.Generic;

    using Mono.Cecil;

    internal class TypeNameComparer : IEqualityComparer<TypeDefinition>
    {
        #region IEqualityComparer<TypeDefinition> Members

        public bool Equals(TypeDefinition x, TypeDefinition y)
        {
            return x.FullName == y.FullName;
        }

        public int GetHashCode(TypeDefinition obj)
        {
            return obj.Name.GetHashCode();
        }

        #endregion
    }
}