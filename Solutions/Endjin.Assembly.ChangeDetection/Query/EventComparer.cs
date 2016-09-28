using System.Collections.Generic;
using Endjin.Assembly.ChangeDetection.Introspection;
using Mono.Cecil;

namespace Endjin.Assembly.ChangeDetection.Query
{
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