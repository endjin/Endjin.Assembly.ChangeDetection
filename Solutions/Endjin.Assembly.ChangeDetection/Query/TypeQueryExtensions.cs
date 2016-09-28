using System.Collections.Generic;
using Mono.Cecil;

namespace Endjin.Assembly.ChangeDetection.Query
{
    public static class TypeQueryExtensions
    {
        public static IEnumerable<TypeDefinition> GetMatchingTypes(this List<TypeQuery> list, AssemblyDefinition assembly)
        {
            foreach (var query in list)
            {
                var matchingTypes = query.GetTypes(assembly);
                foreach (var matchingType in matchingTypes)
                {
                    yield return matchingType;
                }
            }
        }
    }
}