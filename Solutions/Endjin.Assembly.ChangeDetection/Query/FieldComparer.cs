using System.Collections.Generic;
using Endjin.Assembly.ChangeDetection.Introspection;
using Mono.Cecil;

namespace Endjin.Assembly.ChangeDetection.Query
{
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