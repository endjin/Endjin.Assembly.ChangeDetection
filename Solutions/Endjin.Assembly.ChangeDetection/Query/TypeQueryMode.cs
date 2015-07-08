namespace AssemblyDifferences.Query
{
    using System;

    [Flags]
    public enum TypeQueryMode
    {
        None = 0,

        Public = 1,

        Internal = 2,

        NotCompilerGenerated = 4,

        Interface = 8,

        Class = 16,

        ValueType = 32,

        Enum = 64,

        ApiRelevant = Public | NotCompilerGenerated | Interface | Class | ValueType | Enum,

        All = Public | Internal | Interface | Class | ValueType | Enum
    }
}