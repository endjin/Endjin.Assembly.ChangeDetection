using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace Endjin.Assembly.ChangeDetection.Query
{
    public class QueryAggregator
    {
        public List<EventQuery> EventQueries = new List<EventQuery>();

        public List<FieldQuery> FieldQueries = new List<FieldQuery>();

        public List<MethodQuery> MethodQueries = new List<MethodQuery>();

        public List<TypeQuery> TypeQueries = new List<TypeQuery>();

        /// <summary>
        ///     Contains also internal types, fields and methods since the InteralsVisibleToAttribute
        ///     can open visibility
        /// </summary>
        public static QueryAggregator PublicApiQueries
        {
            get
            {
                var agg = new QueryAggregator();

                agg.TypeQueries.Add(new TypeQuery(TypeQueryMode.ApiRelevant));

                agg.MethodQueries.Add(MethodQuery.PublicMethods);
                agg.MethodQueries.Add(MethodQuery.ProtectedMethods);

                agg.FieldQueries.Add(FieldQuery.PublicFields);
                agg.FieldQueries.Add(FieldQuery.ProtectedFields);

                agg.EventQueries.Add(EventQuery.PublicEvents);
                agg.EventQueries.Add(EventQuery.ProtectedEvents);

                return agg;
            }
        }

        public static QueryAggregator AllExternallyVisibleApis
        {
            get
            {
                var agg = PublicApiQueries;
                agg.TypeQueries.Add(new TypeQuery(TypeQueryMode.Internal));
                agg.MethodQueries.Add(MethodQuery.InternalMethods);
                agg.FieldQueries.Add(FieldQuery.InteralFields);
                agg.EventQueries.Add(EventQuery.InternalEvents);
                return agg;
            }
        }

        public List<TypeDefinition> ExeuteAndAggregateTypeQueries(AssemblyDefinition assembly)
        {
            var result = new List<TypeDefinition>();
            foreach (var query in this.TypeQueries)
            {
                result.AddRange(query.GetTypes(assembly));
            }

            var distinctResults = result;
            if (this.TypeQueries.Count > 1)
            {
                distinctResults = result.Distinct(new TypeNameComparer()).ToList();
            }

            return distinctResults;
        }

        public List<MethodDefinition> ExecuteAndAggregateMethodQueries(TypeDefinition type)
        {
            var methods = new List<MethodDefinition>();
            foreach (var query in this.MethodQueries)
            {
                methods.AddRange(query.GetMethods(type));
            }

            var distinctResults = methods;
            if (this.MethodQueries.Count > 1)
            {
                distinctResults = methods.Distinct(new MethodComparer()).ToList();
            }

            return distinctResults;
        }

        public List<FieldDefinition> ExecuteAndAggregateFieldQueries(TypeDefinition type)
        {
            var fields = new List<FieldDefinition>();
            foreach (var query in this.FieldQueries)
            {
                fields.AddRange(query.GetMatchingFields(type));
            }

            var distinctResults = fields;
            if (this.FieldQueries.Count > 1)
            {
                distinctResults = fields.Distinct(new FieldComparer()).ToList();
            }

            return distinctResults;
        }

        public List<EventDefinition> ExecuteAndAggregateEventQueries(TypeDefinition type)
        {
            var ret = new List<EventDefinition>();

            foreach (var query in this.EventQueries)
            {
                ret.AddRange(query.GetMatchingEvents(type));
            }

            var distinctEvents = ret;
            if (this.EventQueries.Count > 1)
            {
                distinctEvents = ret.Distinct(new EventComparer()).ToList();
            }

            return distinctEvents;
        }
    }
}