using System;

namespace Endjin.Assembly.ChangeDetection.Introspection
{
    [Flags]
    public enum FilterMode
    {
        Private = 1,

        Public = 2,

        Internal = 4,

        Protected = 8,

        NotInternalProtected = 16,

        All = Private | Public | Internal | Protected
    }
}