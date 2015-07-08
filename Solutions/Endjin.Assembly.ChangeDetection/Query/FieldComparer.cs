namespace AssemblyDifferences.Query
{
    using System.Collections.Generic;

    using AssemblyDifferences.Introspection;

    using Mono.Cecil;

    internal class FieldComparer : IEqualityComparer<FieldDefinition>
    {
        #region IEqualityComparer<FieldDefinition> Members

        public bool Equals(FieldDefinition x, FieldDefinition y)
        {
            return x.IsEqual(y);
        }

        public int GetHashCode(FieldDefinition obj)
        {
            return obj.Name.GetHashCode();
        }

        #endregion
    }
}