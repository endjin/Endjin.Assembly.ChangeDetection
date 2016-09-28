using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Mono.Cecil;

namespace Endjin.Assembly.ChangeDetection.Query
{
    public class FieldQuery : BaseQuery
    {
        private const string All = " * *";

        private static readonly FieldQuery myAllFields = new FieldQuery();

        private static readonly FieldQuery myAllFieldsIncludingCompilerGenerated = new FieldQuery("!nocompilergenerated * *");

        private static readonly FieldQuery myPublicFields = new FieldQuery("public " + All);

        private static readonly FieldQuery myProtectedFields = new FieldQuery("protected " + All);

        private static readonly FieldQuery myInternalFields = new FieldQuery("internal " + All);

        private static readonly FieldQuery myPrivateFields = new FieldQuery("private " + All);

        private bool myExcludeCompilerGeneratedFields;

        private bool? myIsConst;

        private bool? myIsReadonly;

        /// <summary>
        ///     Searches for all fields in a class
        /// </summary>
        public FieldQuery() : this("* *")
        {
        }

        /// <summary>
        ///     Queries for specific fields in a class
        /// </summary>
        /// <param name="query">Query string</param>
        /// <remarks>
        ///     The field query must contain at least the field type and name to query for. Access modifier
        ///     are optional
        ///     Example:
        ///     public * *
        ///     protectd * *
        ///     static readonly protected * *
        ///     string m_*
        ///     * my* // Get all fields which field name begins with my
        /// </remarks>
        public FieldQuery(string query) : base(query)
        {
            this.Parser = FieldQueryParser;

            var match = this.Parser.Match(query);
            if (!match.Success)
            {
                throw new ArgumentException(string.Format("The field query string {0} was not a valid query.", query));
            }

            this.myExcludeCompilerGeneratedFields = true;
            this.SetModifierFilter(match);
            this.FieldTypeFilter = GenericTypeMapper.ConvertClrTypeNames(this.Value(match, "fieldType"));

            if (!this.FieldTypeFilter.StartsWith("*"))
            {
                this.FieldTypeFilter = "*" + this.FieldTypeFilter;
            }

            if (this.FieldTypeFilter == "*")
            {
                this.FieldTypeFilter = null;
            }

            this.NameFilter = this.Value(match, "fieldName");
        }

        private string FieldTypeFilter { get; set; }

        public static FieldQuery AllFields
        {
            get
            {
                return myAllFields;
            }
        }

        public static FieldQuery AllFieldsIncludingCompilerGenerated
        {
            get
            {
                return myAllFieldsIncludingCompilerGenerated;
            }
        }

        public static FieldQuery PublicFields
        {
            get
            {
                return myPublicFields;
            }
        }

        public static FieldQuery ProtectedFields
        {
            get
            {
                return myProtectedFields;
            }
        }

        public static FieldQuery InteralFields
        {
            get
            {
                return myInternalFields;
            }
        }

        public static FieldQuery PrivateFields
        {
            get
            {
                return myPrivateFields;
            }
        }

        protected override void SetModifierFilter(Match match)
        {
            base.SetModifierFilter(match);
            this.myIsReadonly = this.Captures(match, "readonly");
            this.myIsConst = this.Captures(match, "const");
            var excludeCompilerGenerated = this.Captures(match, "nocompilergenerated");
            this.myExcludeCompilerGeneratedFields = excludeCompilerGenerated == null ? true : excludeCompilerGenerated.Value;
        }

        public List<FieldDefinition> GetMatchingFields(TypeDefinition type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            var retFields = new List<FieldDefinition>();

            foreach (FieldDefinition field in type.Fields)
            {
                if (this.Match(field, type))
                {
                    retFields.Add(field);
                }
            }

            return retFields;
        }

        internal bool Match(FieldDefinition field, TypeDefinition type)
        {
            var lret = true;

            lret = this.MatchFieldModifiers(field);

            if (lret)
            {
                lret = this.MatchFieldType(field);
            }

            if (lret)
            {
                lret = this.MatchName(field.Name);
            }

            if (lret && this.myExcludeCompilerGeneratedFields)
            {
                lret = !this.IsEventFieldOrPropertyBackingFieldOrEnumBackingField(field, type);
            }

            return lret;
        }

        private bool IsEventFieldOrPropertyBackingFieldOrEnumBackingField(FieldDefinition field, TypeDefinition def)
        {
            // Is Property backing field
            if (field.Name.EndsWith(">k__BackingField"))
            {
                return true;
            }

            if (field.IsSpecialName)
            {
                return true;
            }

            // Is event backing field for event delegate
            foreach (EventDefinition ev in def.Events)
            {
                if (ev.Name == field.Name)
                {
                    return true;
                }
            }

            return false;
        }

        private bool MatchFieldType(FieldDefinition field)
        {
            if (string.IsNullOrEmpty(this.FieldTypeFilter) || this.FieldTypeFilter == "*")
            {
                return true;
            }

            return Matcher.MatchWithWildcards(this.FieldTypeFilter, field.FieldType.FullName, StringComparison.OrdinalIgnoreCase);
        }

        private bool MatchFieldModifiers(FieldDefinition field)
        {
            var lret = true;

            if (lret && this.myIsConst.HasValue) // Literal fields are always constant so there is no need to make a distinction here
            {
                lret = this.myIsConst == field.HasConstant;
            }

            if (lret && this.myIsInternal.HasValue)
            {
                lret = this.myIsInternal == field.IsAssembly;
            }

            if (lret && this.myIsPrivate.HasValue)
            {
                lret = this.myIsPrivate == field.IsPrivate;
            }

            if (lret && this.myIsProtected.HasValue)
            {
                lret = this.myIsProtected == field.IsFamily;
            }

            if (lret && this.myIsProtectedInernal.HasValue)
            {
                lret = this.myIsProtectedInernal == field.IsFamilyOrAssembly;
            }

            if (lret && this.myIsPublic.HasValue)
            {
                lret = this.myIsPublic == field.IsPublic;
            }

            if (lret && this.myIsReadonly.HasValue)
            {
                lret = this.myIsReadonly == field.IsInitOnly;
            }

            if (lret && this.myIsStatic.HasValue)
            {
                lret = this.myIsStatic == (field.IsStatic && !field.HasConstant);
            }

            return lret;
        }
    }
}