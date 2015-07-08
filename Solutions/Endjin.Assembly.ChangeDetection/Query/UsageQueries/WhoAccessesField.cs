namespace AssemblyDifferences.Query.usagequeries
{
    using System;
    using System.Collections.Generic;

    using AssemblyDifferences.Introspection;

    using Mono.Cecil;
    using Mono.Cecil.Cil;

    public class WhoAccessesField : UsageVisitor
    {
        private readonly HashSet<string> myDeclaringTypeNamesToSearch = new HashSet<string>();

        private readonly List<FieldDefinition> mySearchFields;

        public WhoAccessesField(UsageQueryAggregator aggreagator, FieldDefinition field) : this(aggreagator, new List<FieldDefinition>
        {
            ThrowIfNull("field", field)
        })
        {
        }

        public WhoAccessesField(UsageQueryAggregator aggregator, List<FieldDefinition> fields) : base(aggregator)
        {
            if (fields == null)
            {
                throw new ArgumentException("The field list was null.");
            }

            this.mySearchFields = fields;

            foreach (var field in fields)
            {
                if (field.HasConstant)
                {
                    throw new ArgumentException(string.Format("The field {0} is constant. Its value is compiled directly into the users of this constant which makes is impossible to search for users of it.", field.Print(FieldPrintOptions.All)));
                }
                this.myDeclaringTypeNamesToSearch.Add(field.DeclaringType.Name);
                this.Aggregator.AddVisitScope(field.DeclaringType.Module.Assembly.Name.Name);
            }
        }

        private void CheckFieldReferenceAndAddIfMatch(Instruction instr, MethodDefinition method, string operation)
        {
            var field = (FieldReference)instr.Operand;

            if (this.myDeclaringTypeNamesToSearch.Contains(field.DeclaringType.Name))
            {
                foreach (var searchField in this.mySearchFields)
                {
                    if (field.DeclaringType.IsEqual(searchField.DeclaringType, false) && field.Name == searchField.Name && field.FieldType.IsEqual(searchField.FieldType))
                    {
                        var context = new MatchContext(operation, string.Format("{0} {1}", searchField.DeclaringType.FullName, searchField.Name));
                        this.Aggregator.AddMatch(instr, method, false, context);
                    }
                }
            }
        }

        public override void VisitMethod(MethodDefinition method)
        {
            if (!method.HasBody)
            {
                return;
            }

            foreach (Instruction instr in method.Body.Instructions)
            {
                switch (instr.OpCode.Code)
                {
                    case Code.Ldfld: // Load instance field value
                        this.CheckFieldReferenceAndAddIfMatch(instr, method, "Read");
                        break;
                    case Code.Ldflda: // Load instance field address
                        this.CheckFieldReferenceAndAddIfMatch(instr, method, "Load Address");
                        break;
                    case Code.Ldsflda: // Load static field address
                        this.CheckFieldReferenceAndAddIfMatch(instr, method, "Load Address");
                        break;
                    case Code.Ldsfld: // Load static field value
                        this.CheckFieldReferenceAndAddIfMatch(instr, method, "Read");
                        break;
                    case Code.Stfld: // Store field
                        this.CheckFieldReferenceAndAddIfMatch(instr, method, "Assign");
                        break;
                    case Code.Stsfld: // Store static field
                        this.CheckFieldReferenceAndAddIfMatch(instr, method, "Assign");
                        break;

                    default:
                        break;
                }
            }
        }
    }
}