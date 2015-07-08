namespace AssemblyDifferences.Query
{
    using System.Collections.Generic;

    using AssemblyDifferences.Introspection;

    using Mono.Cecil;

    internal class MethodComparer : IEqualityComparer<MethodDefinition>
    {
        #region IEqualityComparer<MethodDefinition> Members

        public bool Equals(MethodDefinition x, MethodDefinition y)
        {
            return x.IsEqual(y);
        }

        public int GetHashCode(MethodDefinition obj)
        {
            return obj.Name.GetHashCode();
        }

        #endregion
    }
}