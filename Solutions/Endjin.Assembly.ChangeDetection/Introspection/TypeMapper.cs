namespace AssemblyDifferences.Introspection
{
    using System.Collections.Generic;

    public class TypeMapper
    {
        private static readonly Dictionary<string, string> mySimpleType2FullType = new Dictionary<string, string>
        {
            { "bool", "System.Boolean" },
            { "byte", "System.Byte" },
            { "sbyte", "System.SByte" },
            { "char", "System.Char" },
            { "decimal", "System.Decimal" },
            { "double", "System.Double" },
            { "float", "System.Single" },
            { "int", "System.Int32" },
            { "uint", "System.UInt32" },
            { "long", "System.Int64" },
            { "ulong", "System.UInt64" },
            { "object", "System.Object" },
            { "short", "System.Int16" },
            { "ushort", "System.UInt16" },
            { "string", "System.String" },
            { "", "System.Void" },
            { "void", "System.Void" },

            // for system reflection compat support this shorthand notation as well
            { "Bool", "System.Boolean" },
            { "Byte", "System.Byte" },
            { "SByte", "System.SByte" },
            { "Char", "System.Char" },
            { "Decimal", "System.Decimal" },
            { "Double", "System.Double" },
            { "Single", "System.Single" },
            { "Int32", "System.Int32" },
            { "UInt32", "System.UInt32" },
            { "Int64", "System.Int64" },
            { "UInt64", "System.UInt64" },
            { "Object", "System.Object" },
            { "Int16", "System.Int16" },
            { "UInt16", "System.UInt16" },
            { "String", "System.String" },
            { "Void", "System.Void" }
        };

        private static readonly Dictionary<string, string> myFullType2SimpleType = new Dictionary<string, string>
        {
            { "System.Boolean", "bool" },
            { "System.Byte", "byte" },
            { "System.SByte", "sbyte" },
            { "System.Char", "char" },
            { "System.Decimal", "decimal" },
            { "System.Double", "double" },
            { "System.Single", "float" },
            { "System.Int32", "int" },
            { "System.UInt32", "uint" },
            { "System.Int64", "long" },
            { "System.UInt64", "ulong" },
            { "System.Object", "object" },
            { "System.Int16", "short" },
            { "System.UInt16", "ushort" },
            { "System.String", "string" },
            { "System.Void", "void" },
            // Generic parameters have not full qualfied type names
            { "Boolean", "bool" },
            { "Byte", "byte" },
            { "SByte", "sbyte" },
            { "Char", "char" },
            { "Decimal", "decimal" },
            { "Double", "double" },
            { "Single", "float" },
            { "Int32", "int" },
            { "UInt32", "uint" },
            { "Int64", "long" },
            { "UInt64", "ulong" },
            { "Object", "object" },
            { "Int16", "short" },
            { "UInt16", "ushort" },
            { "String", "string" },
            { "Void", "void" }
        };

        /// <summary>
        ///     Map a short type e.g int to the full system type System.Int32
        /// </summary>
        /// <param name="shortType">The short type.</param>
        /// <returns>the expanded system type if possible</returns>
        public static string ShortToFull(string shortType)
        {
            var lret = shortType;
            string fullType = null;
            if (mySimpleType2FullType.TryGetValue(shortType, out fullType))
            {
                lret = fullType;
            }
            return lret;
        }

        public static string FullToShort(string fullType)
        {
            var lret = fullType;
            string shortType = null;
            if (myFullType2SimpleType.TryGetValue(fullType, out shortType))
            {
                lret = shortType;
            }

            return lret;
        }
    }
}