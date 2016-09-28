using System.Collections.Generic;
using Endjin.Assembly.ChangeDetection.Introspection;
using Mono.Cecil;

namespace Endjin.Assembly.ChangeDetection.Query
{
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