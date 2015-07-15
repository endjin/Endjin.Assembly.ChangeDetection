namespace AssemblyDifferences.Query
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Text.RegularExpressions;

    using AssemblyDifferences.Introspection;

    using Mono.Cecil;

    public class MethodQuery : BaseQuery
    {
        protected internal bool? myIsVirtual;

        public static MethodQuery AllMethods
        {
            get { return new MethodQuery(); }
        }

        const string All = " * *(*)";

        public static MethodQuery ProtectedMethods
        {
            get { return new MethodQuery("protected " + All); }
        }

        public static MethodQuery InternalMethods
        {
            get { return new MethodQuery("internal " + All); }
        }

        public static MethodQuery PublicMethods
        {
            get { return new MethodQuery("public " + All); }
        }

        public static MethodQuery PrivateMethods
        {
            get { return new MethodQuery("private " + All); }
        }

        internal Regex ReturnTypeFilter
        {
            get;
            set;
        }

        internal List<KeyValuePair<Regex, string>> ArgumentFilters
        {
            get;
            set;
        }

        static char[] myArgTrimChars = new char[] { '[', ']', ',' };

        /// <summary>
        /// Create a method query which matches every method
        /// </summary>
        public MethodQuery()
            : this("*")
        {
        }

        /// <summary>
        /// Create a new instance of a Query to match for specific methods for a given type.
        /// </summary>
        /// <remarks>The query format can be a simple string like
        /// * // get everything
        /// public void Function(int firstArg, bool secondArg)  // match specfic method
        /// public * *( * ) // match all public methods
        /// protected * *(* a) // match all protected methods with one parameter
        /// </remarks>
        /// <param name="methodQuery">The method query.</param>
        public MethodQuery(string methodQuery)
            : base(methodQuery)
        {

            // Return everything if no filter is set
            if (methodQuery.Trim() == "*")
            {
                return;
            }

            // Get cached instance
            Parser = MethodDefParser;

            // otherwise we expect a filter query that looks like a function definition
            Match m = Parser.Match(methodQuery.Trim());

            if (!m.Success)
            {
                throw new ArgumentException(String.Format("Invalid method query: \"{0}\". The method query must be of the form <modifier> <return type> <function name>(<arguments>) e.g. public void F(*) match all public methods with name F with 0 or more arguments, or public * *(*) match any public method.", methodQuery));
            }

            CreateReturnTypeFilter(m);

            NameFilter = m.Groups["funcName"].Value;
            int idx = NameFilter.IndexOf('<');
            if (idx != -1)
                NameFilter = NameFilter.Substring(0, idx);

            if (String.IsNullOrEmpty(NameFilter))
            {
                NameFilter = null;
            }

            this.ArgumentFilters = InitArgumentFilter(m.Groups["args"].Value);

            SetModifierFilter(m);
        }

        private void CreateReturnTypeFilter(Match m)
        {
            string filter = m.Groups["retType"].Value.Replace(" ", "");

            if (!String.IsNullOrEmpty(filter))
            {
                ReturnTypeFilter = CreateRegularExpressionFromTypeString(filter);
            }
        }

        protected override void SetModifierFilter(Match m)
        {
            base.SetModifierFilter(m);
            myIsVirtual = Captures(m, "virtual");
        }

        protected bool MatchMethodModifiers(MethodDefinition method)
        {
            bool lret = true;

            if (myIsPublic.HasValue)
                lret = method.IsPublic == myIsPublic;
            if (lret && myIsInternal.HasValue)
                lret = method.IsAssembly == myIsInternal;
            if (lret && myIsPrivate.HasValue)
                lret = method.IsPrivate == myIsPrivate;
            if (lret && myIsProtectedInernal.HasValue)
                lret = method.IsFamilyOrAssembly == myIsProtectedInernal;
            if (lret && myIsProtected.HasValue)
                lret = method.IsFamily == myIsProtected;
            if (lret && myIsVirtual.HasValue)
                lret = method.IsVirtual == myIsVirtual;
            if (lret && myIsStatic.HasValue)
                lret = method.IsStatic == myIsStatic;

            return lret;
        }

        internal List<KeyValuePair<Regex, string>> InitArgumentFilter(string argFilter)
        {
            if (argFilter == null || argFilter == "*")
                return null;

            // To query for void methods
            if (argFilter == "")
                return new List<KeyValuePair<Regex, string>>();

            int inGeneric = 0;

            bool bIsType = true;
            List<KeyValuePair<Regex, string>> list = new List<KeyValuePair<Regex, string>>();
            StringBuilder curThing = new StringBuilder();
            string curType = null;
            string curArgName = null;

            char prev = '\0';
            char current;
            for (int i = 0; i < argFilter.Length; i++)
            {
                current = argFilter[i];

                if (current != ' ')
                    curThing.Append(current);

                if ('<' == current)
                {
                    inGeneric++;
                }
                else if ('>' == current)
                {
                    inGeneric--;
                }

                if (inGeneric > 0)
                    continue;

                if (i > 0)
                    prev = argFilter[i - 1];

                // ignore subsequent spaces
                if (' ' == current && prev == ' ')
                {
                    continue;
                }

                // Got end of file argument name
                if (',' == current && curThing.Length > 0)
                {
                    curThing.Remove(curThing.Length - 1, 1);
                    curArgName = curThing.ToString().Trim();
                    curThing.Length = 0;

                    if (curType == null || curArgName == null)
                    {
                        throw new ArgumentException(
                            String.Format("Method argument filter is of wrong format: {0}", argFilter));
                    }

                    list.Add(AssignArrayBracketsToTypeName(curType, curArgName));
                    curType = null;
                    curArgName = null;

                    bIsType = true;
                }

                if (current == ' ' && curThing.Length > 0 && bIsType != false)
                {
                    curType = GenericTypeMapper.ConvertClrTypeNames(curThing.ToString().Trim());
                    curThing.Length = 0;
                    bIsType = false;
                }
            }

            if (curType != null)
            {
                list.Add(AssignArrayBracketsToTypeName(curType, curThing.ToString().Trim()));
            }


            return list;
        }

        KeyValuePair<Regex, string> AssignArrayBracketsToTypeName(string typeName, string argName)
        {
            string newTypeName = typeName;
            string newArgName = argName;

            if (argName.StartsWith("["))
            {
                newTypeName += argName.Substring(0, argName.LastIndexOf(']') + 1);
                newArgName = newArgName.Trim(myArgTrimChars);
            }

            newArgName = PrependStarToFilter(newArgName);
            Regex typeFilter = CreateRegularExpressionFromTypeString(newTypeName);

            return new KeyValuePair<Regex, string>(typeFilter, newArgName);
        }

        private Regex CreateRegularExpressionFromTypeString(string newTypeName)
        {
            newTypeName = CreateRegexFilterFromTypeName(newTypeName);
            if (newTypeName.StartsWith("*"))
            {
                newTypeName = "." + newTypeName;
            }

            newTypeName = GenericTypeMapper.TransformGenericTypeNames(newTypeName, CreateRegexFilterFromTypeName);

            newTypeName = Regex.Escape(newTypeName);
            // unescape added wild cards
            newTypeName = newTypeName.Replace("\\.\\*", ".*");
            return new Regex(newTypeName, RegexOptions.IgnoreCase); ;
        }

        string CreateRegexFilterFromTypeName(string filterstr)
        {
            if (!String.IsNullOrEmpty(filterstr))
            {
                if (!filterstr.StartsWith(".*") && !filterstr.StartsWith("*"))
                {
                    return ".*" + filterstr;
                }
            }
            return filterstr;
        }

        string PrependStarToFilter(string filterstr)
        {
            if (!String.IsNullOrEmpty(filterstr))
            {
                if (!filterstr.StartsWith("*"))
                {
                    return "*" + filterstr;
                }
            }
            return filterstr;
        }

        internal bool MatchReturnType(MethodDefinition method)
        {
            if (this.ReturnTypeFilter == null)
            {
                return true;
            }

            return ReturnTypeFilter.IsMatch(method.ReturnType.FullName);
        }

        bool IsArgumentMatch(Regex typeFilter, string argNameFilter, string typeName, string argName)
        {
            bool lret = true;

            Match m = typeFilter.Match(typeName);
            lret = m.Success;
            if (lret)
            {
                lret = Matcher.MatchWithWildcards(argNameFilter, argName, StringComparison.OrdinalIgnoreCase);
            }

            return lret;
        }


        internal bool MatchArguments(MethodDefinition method)
        {
            // Query all methods regardless number of parameters
            if (this.ArgumentFilters == null)
                return true;

            if (this.ArgumentFilters.Count != method.Parameters.Count)
                return false;

            for (int i = 0; i < ArgumentFilters.Count; i++)
            {
                ParameterDefinition curDef = method.Parameters[i];
                KeyValuePair<Regex, string> curFilters = ArgumentFilters[i];

                if (!IsArgumentMatch(curFilters.Key, curFilters.Value, curDef.ParameterType.FullName, curDef.Name))
                {
                    return false;
                }
            }

            return true;
        }



        public MethodDefinition GetSingleMethod(TypeDefinition type)
        {
            var matches = GetMethods(type);
            if (matches.Count > 1)
                throw new InvalidOperationException(String.Format("Got more than one matching method: {0}", matches.Count));

            if (matches.Count == 0)
                return null;

            return matches[0];
        }

        public virtual List<MethodDefinition> GetMethods(TypeDefinition type)
        {
            List<MethodDefinition> matchingMethods = new List<MethodDefinition>();
            foreach (MethodDefinition method in type.Methods)
            {
                if (Match(type, method))
                    matchingMethods.Add(method);
            }

            return matchingMethods;
        }


        internal bool Match(TypeDefinition type, MethodDefinition method)
        {
            bool lret = MatchMethodModifiers(method);

            if (lret)
            {
                lret = MatchName(method.Name);
                if (method.Name == ".ctor")
                {
                    lret = MatchName(method.DeclaringType.Name);
                }
            }

            if (lret)
            {
                lret = MatchReturnType(method);
            }

            if (lret)
            {
                lret = MatchArguments(method);
            }

            if (lret)
            {
                lret = IsNoEventMethod(type, method);
            }

            return lret;
        }

        private bool IsNoEventMethod(TypeDefinition type, MethodDefinition method)
        {
            bool lret = true;
            if (method.IsSpecialName) // Is usually either a property or event add/remove method
            {
                foreach (EventDefinition ev in type.Events)
                {
                    if (ev.AddMethod.IsEqual(method) ||
                        ev.RemoveMethod.IsEqual(method))
                    {
                        lret = false;
                        break;
                    }
                }
            }

            return lret;
        }
    }
}