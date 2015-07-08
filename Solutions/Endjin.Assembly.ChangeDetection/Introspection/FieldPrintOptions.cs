namespace AssemblyDifferences.Introspection
{
    using System;

    [Flags]
    public enum FieldPrintOptions
    {
        Visibility = 1,

        Modifiers = 2,

        SimpleType = 4,

        Value = 8,

        All = Visibility | Modifiers | SimpleType | Value
    }
}