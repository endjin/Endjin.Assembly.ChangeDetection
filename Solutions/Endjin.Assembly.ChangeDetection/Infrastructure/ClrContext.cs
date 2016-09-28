using System;

namespace Endjin.Assembly.ChangeDetection.Infrastructure
{
    /// <summary>
    ///     There are currently 4 combinations possible but I want to
    ///     stay extensible since some .NET Framework patch might need
    ///     additional treatement.
    /// </summary>
    [Flags]
    internal enum ClrContext
    {
        None,

        Is32Bit = 1,

        Is64Bit = 2,

        IsNet2 = 4,

        IsNet4 = 8
    }
}