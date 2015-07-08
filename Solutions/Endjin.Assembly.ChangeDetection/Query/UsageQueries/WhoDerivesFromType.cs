namespace AssemblyDifferences.Query.usagequeries
{
    using System;
    using System.Collections.Generic;

    using AssemblyDifferences.Introspection;

    using Mono.Cecil;

    public class WhoDerivesFromType : UsageVisitor
    {
        private readonly List<TypeDefinition> mySearchBaseTypes;

        public WhoDerivesFromType(UsageQueryAggregator aggregator, TypeDefinition typeDef) : this(aggregator, new List<TypeDefinition>
        {
            ThrowIfNull("typeDef", typeDef)
        })
        {
        }

        public WhoDerivesFromType(UsageQueryAggregator aggregator, List<TypeDefinition> typeDefs) : base(aggregator)
        {
            if (typeDefs == null)
            {
                throw new ArgumentException("The type list to query for was null.");
            }

            foreach (var type in typeDefs)
            {
                this.Aggregator.AddVisitScope(type.Module.Assembly.Name.Name);
            }

            this.mySearchBaseTypes = typeDefs;
        }

        public override void VisitType(TypeDefinition type)
        {
            if (type.BaseType == null)
            {
                return;
            }

            foreach (var searchType in this.mySearchBaseTypes)
            {
                if (type.BaseType.IsEqual(searchType, false))
                {
                    var context = new MatchContext("Derives from", searchType.Print());
                    this.Aggregator.AddMatch(type, context);
                    break;
                }
            }
        }
    }
}