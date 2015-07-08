namespace AssemblyDifferences.Infrastructure
{
    using System;

    /// <summary>
    ///     Trace levels where 1 is the one with only high level traces whereas 5 is the level with
    ///     highest details. Trace levels can be combined together so you can look for all high level messages only
    ///     and all errors at all levels.
    /// </summary>
    [Flags]
    public enum Level
    {
        None = 0,

        L1 = 1,

        L2 = 2,

        L3 = 4,

        L4 = 8,

        L5 = 16,

        Dispose = 32,

        All = L1 | L2 | L3 | L4 | L5 | Dispose
    }
}