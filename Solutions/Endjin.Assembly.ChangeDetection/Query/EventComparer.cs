namespace AssemblyDifferences.Query
{
    using System.Collections.Generic;

    using AssemblyDifferences.Introspection;

    using Mono.Cecil;

    internal class EventComparer : IEqualityComparer<EventDefinition>
    {
        #region IEqualityComparer<EventDefinition> Members

        public bool Equals(EventDefinition x, EventDefinition y)
        {
            return x.AddMethod.IsEqual(y.AddMethod);
        }

        public int GetHashCode(EventDefinition obj)
        {
            return obj.AddMethod.Name.GetHashCode();
        }

        #endregion
    }
}