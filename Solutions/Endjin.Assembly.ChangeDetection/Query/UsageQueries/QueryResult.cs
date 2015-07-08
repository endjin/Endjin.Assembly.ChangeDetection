namespace AssemblyDifferences.Query.usagequeries
{
    using System;

    using Mono.Cecil;

    public class QueryResult<T>
        where T : IMemberReference
    {
        private MatchContext myAnnotations;

        public QueryResult(T match, string fileName, int lineNumber)
        {
            if (match == null)
            {
                throw new ArgumentNullException("result");
            }

            this.Match = match;
            this.SourceFileName = fileName;
            this.LineNumber = lineNumber;
        }

        public QueryResult(T match, string fileName, int lineNumber, MatchContext context) : this(match, fileName, lineNumber)
        {
            if (context == null)
            {
                throw new ArgumentNullException("match context was null");
            }

            foreach (var kvp in context)
            {
                this.Annotations[kvp.Key] = kvp.Value;
            }
        }

        public T Match { get; private set; }

        public string SourceFileName { get; private set; }

        public int LineNumber { get; private set; }

        public MatchContext Annotations
        {
            get
            {
                if (this.myAnnotations == null)
                {
                    this.myAnnotations = new MatchContext();
                }

                return this.myAnnotations;
            }
        }
    }
}