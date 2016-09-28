using System;

namespace Endjin.Assembly.ChangeDetection.Infrastructure
{
    /// <summary>
    ///     Severity of trace messages
    /// </summary>
    [Flags]
    public enum MessageTypes
    {
        None = 0,

        Info = 1,

        Instrument = 2,

        Warning = 4,

        Error = 8,

        InOut = 16,

        Exception = 32,

        All = InOut | Info | Instrument | Warning | Error | Exception
    }
}