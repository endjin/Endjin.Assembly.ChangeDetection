namespace AssemblyDifferences.Query.usagequeries
{
    using System;

    using Mono.Cecil;
    using Mono.Cecil.Cil;

    /// <summary>
    ///     Search for the ldstr opcode which loads a string constant and check if the string constant matches
    ///     the search string.
    /// </summary>
    public class WhoUsesStringConstant : UsageVisitor
    {
        private readonly bool mybExatchMatch;

        private readonly StringComparison myComparisonMode;

        private readonly string mySearchString;

        public WhoUsesStringConstant(UsageQueryAggregator aggregator, string searchString, bool bExactMatch, StringComparison compMode) : base(aggregator)
        {
            if (string.IsNullOrEmpty(searchString))
            {
                throw new ArgumentException("The search string was null or empty");
            }

            this.mySearchString = searchString;
            this.mybExatchMatch = bExactMatch;
            this.myComparisonMode = compMode;
        }

        public WhoUsesStringConstant(UsageQueryAggregator aggregator, string searchString) : this(aggregator, searchString, false, StringComparison.OrdinalIgnoreCase)
        {
        }

        public WhoUsesStringConstant(UsageQueryAggregator aggregator, string searchString, bool bExactMatch) : this(aggregator, searchString, bExactMatch, StringComparison.OrdinalIgnoreCase)
        {
        }

        public override void VisitMethodBody(MethodBody body)
        {
            base.VisitMethodBody(body);
            foreach (Instruction instruction in body.Instructions)
            {
                if (instruction.OpCode.Code == Code.Ldstr)
                {
                    var str = (string)instruction.Operand;
                    if (this.IsMatch(str))
                    {
                        this.AddMatch(instruction, body, str);
                    }
                }
            }
        }

        private bool IsMatch(string value)
        {
            var lret = false;

            if (this.mybExatchMatch)
            {
                if (string.Compare(value, this.mySearchString, this.myComparisonMode) == 0)
                {
                    lret = true;
                }
            }
            else
            {
                if (value.IndexOf(this.mySearchString, this.myComparisonMode) != -1)
                {
                    lret = true;
                }
            }

            return lret;
        }

        public override void VisitField(FieldDefinition field)
        {
            base.VisitField(field);

            if (field.HasConstant)
            {
                var stringValue = field.Constant as string;
                if (stringValue != null && this.IsMatch(stringValue))
                {
                    var context = new MatchContext("String Match", this.mySearchString);
                    context["String"] = stringValue;
                    this.Aggregator.AddMatch(field, context);
                }
            }
        }

        private void AddMatch(Instruction ins, MethodBody body, string matchedString)
        {
            var context = new MatchContext("String Match", this.mySearchString);
            context["String"] = matchedString;
            this.Aggregator.AddMatch(ins, body.Method, false, context);
        }
    }
}