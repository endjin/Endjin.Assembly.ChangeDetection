namespace AssemblyDifferences.Query
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    /// <summary>
    ///     Parser for a list of type queries separated by ;. A type query can
    /// </summary>
    internal class TypeQueryFactory
    {
        private static readonly Regex myQueryParser = new Regex("^ *(?<modifiers>api +|nocompiler +|public +|internal +|class +|struct +|interface +|enum +)* *(?<typeName>[^ ]+) *$");

        /// <summary>
        ///     Parse a list of type queries separated by ; and return the resulting type query list
        /// </summary>
        /// <param name="queries"></param>
        /// <returns></returns>
        public List<TypeQuery> GetQueries(string queries)
        {
            return this.GetQueries(queries, TypeQueryMode.None);
        }

        public List<TypeQuery> GetQueries(string typeQueries, TypeQueryMode additionalFlags)
        {
            if (typeQueries == null)
            {
                throw new ArgumentNullException("typeQueries");
            }

            var trimedQuery = typeQueries.Trim();
            if (trimedQuery == "")
            {
                throw new ArgumentException("typeQueries was an empty string");
            }

            var queries = trimedQuery.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            var ret = new List<TypeQuery>();

            foreach (var query in queries)
            {
                var m = myQueryParser.Match(query);
                if (!m.Success)
                {
                    throw new ArgumentException(string.Format("The type query \"{0}\" is not of the form [public|internal|class|interface|struct|enum|nocompiler|api] typename", query));
                }

                var mode = this.GetQueryMode(m);
                var nameSpaceTypeName = this.SplitNameSpaceAndType(m.Groups["typeName"].Value);
                var typeQuery = new TypeQuery(mode, nameSpaceTypeName.Key, nameSpaceTypeName.Value);
                if (typeQuery.SearchMode == TypeQueryMode.None)
                {
                    typeQuery.SearchMode |= additionalFlags;
                }
                ret.Add(typeQuery);
            }

            return ret;
        }

        internal KeyValuePair<string, string> SplitNameSpaceAndType(string fullQualifiedTypeName)
        {
            if (string.IsNullOrEmpty(fullQualifiedTypeName))
            {
                throw new ArgumentNullException("fullQualifiedTypeName");
            }

            var parts = fullQualifiedTypeName.Trim().Split('.');
            if (parts.Length > 1)
            {
                return new KeyValuePair<string, string>(string.Join(".", parts, 0, parts.Length - 1), parts[parts.Length - 1]);
            }
            return new KeyValuePair<string, string>(null, parts[0]);
        }

        private TypeQueryMode GetQueryMode(Match m)
        {
            var mode = TypeQueryMode.None;

            if (this.Captures(m, "public"))
            {
                mode |= TypeQueryMode.Public;
            }
            if (this.Captures(m, "internal"))
            {
                mode |= TypeQueryMode.Internal;
            }
            if (this.Captures(m, "class"))
            {
                mode |= TypeQueryMode.Class;
            }
            if (this.Captures(m, "interface"))
            {
                mode |= TypeQueryMode.Interface;
            }
            if (this.Captures(m, "struct"))
            {
                mode |= TypeQueryMode.ValueType;
            }
            if (this.Captures(m, "enum"))
            {
                mode |= TypeQueryMode.Enum;
            }
            if (this.Captures(m, "nocompiler"))
            {
                mode |= TypeQueryMode.NotCompilerGenerated;
            }
            if (this.Captures(m, "api"))
            {
                mode |= TypeQueryMode.ApiRelevant;
            }

            return mode;
        }

        protected internal virtual bool Captures(Match m, string value)
        {
            foreach (Capture capture in m.Groups["modifiers"].Captures)
            {
                if (value == capture.Value.TrimEnd())
                {
                    return true;
                }
            }

            return false;
        }
    }
}