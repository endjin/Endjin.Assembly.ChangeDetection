using System;
using System.Text.RegularExpressions;

namespace Endjin.Assembly.ChangeDetection.Query
{
    public class BaseQuery
    {
        // Common Regular expression part shared by the different queries
        private const string CommonModifiers = "!?static +|!?public +|!?protected +internal +|!?protected +|!?internal +|!?private +";

        private static Regex myEventQueryParser;

        private static Regex myFieldQueryParser;

        private static Regex myMethodDefParser;

        protected internal bool? myIsInternal;

        protected internal bool? myIsPrivate;

        protected internal bool? myIsProtected;

        protected internal bool? myIsProtectedInernal;

        protected internal bool? myIsPublic;

        protected internal bool? myIsStatic;

        protected BaseQuery(string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                throw new ArgumentNullException("query string was empty");
            }
        }

        internal static Regex EventQueryParser
        {
            get
            {
                if (myEventQueryParser == null)
                {
                    myEventQueryParser = new Regex("^ *(?<modifiers>!?virtual +|event +|" + CommonModifiers + ")*" + @" *(?<eventType>[^ ]+(<.*>)?) +(?<eventName>[^ ]+) *$");
                }

                return myEventQueryParser;
            }
        }

        internal static Regex FieldQueryParser
        {
            get
            {
                if (myFieldQueryParser == null)
                {
                    myFieldQueryParser = new Regex(" *(?<modifiers>!?nocompilergenerated +|!?const +|!?readonly +|" + CommonModifiers + ")*" + @" *(?<fieldType>[^ ]+(<.*>)?) +(?<fieldName>[^ ]+) *$");
                }

                return myFieldQueryParser;
            }
        }

        internal static Regex MethodDefParser
        {
            get
            {
                if (myMethodDefParser == null)
                {
                    myMethodDefParser = new Regex(@" *(?<modifiers>!?virtual +|" + CommonModifiers + ")*" + @"(?<retType>.*<.*>( *\[\])?|[^ (\)]*( *\[\])?) +(?<funcName>.+)\( *(?<args>.*?) *\) *");
                }

                return myMethodDefParser;
            }
        }

        protected internal Regex Parser { get; set; }

        protected internal string NameFilter { get; set; }

        protected internal virtual bool IsMatch(Match m, string key)
        {
            return m.Groups[key].Success;
        }

        protected internal virtual bool? Captures(Match m, string value)
        {
            var notValue = "!" + value;
            foreach (Capture capture in m.Groups["modifiers"].Captures)
            {
                if (value == capture.Value.TrimEnd())
                {
                    return true;
                }
                if (notValue == capture.Value.TrimEnd())
                {
                    return false;
                }
            }

            return null;
        }

        protected string Value(Match m, string groupName)
        {
            return m.Groups[groupName].Value;
        }

        protected virtual void SetModifierFilter(Match m)
        {
            this.myIsProtected = this.Captures(m, "protected");
            this.myIsInternal = this.Captures(m, "internal");
            this.myIsProtectedInernal = this.Captures(m, "protected internal");
            this.myIsPublic = this.Captures(m, "public");
            this.myIsPrivate = this.Captures(m, "private");
            this.myIsStatic = this.Captures(m, "static");
        }

        protected virtual bool MatchName(string name)
        {
            if (string.IsNullOrEmpty(this.NameFilter) || this.NameFilter == "*")
            {
                return true;
            }

            return Matcher.MatchWithWildcards(this.NameFilter, name, StringComparison.OrdinalIgnoreCase);
        }

        private int CountChars(char searchChar, string str)
        {
            var ret = 0;
            foreach (var c in str)
            {
                if (c == searchChar)
                {
                    ret++;
                }
            }

            return ret;
        }
    }
}